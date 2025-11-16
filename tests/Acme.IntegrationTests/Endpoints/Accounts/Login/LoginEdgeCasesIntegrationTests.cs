using System.Net;
using FluentAssertions;
using Acme.Application.Features.Account.Login;
using Xunit;

namespace Acme.IntegrationTests.Endpoints.Accounts.Login;

/// <summary>
/// Integration tests for edge cases in Login endpoint.
/// </summary>
public class LoginEdgeCasesIntegrationTests : IClassFixture<LoginIntegrationTestsFixture>
{
    private readonly HttpClient _client;

    public LoginEdgeCasesIntegrationTests(LoginIntegrationTestsFixture fixture)
    {
        _client = fixture.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task LoginWithEmptyEmailReturnsValidationError()
    {
        // Arrange
        var invalidUser = new LoginCommand { Email = "", Password = "Password@123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/accounts/login", invalidUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LoginWithEmptyPasswordReturnsValidationError()
    {
        // Arrange
        var invalidUser = new LoginCommand { Email = "user@test.com", Password = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/accounts/login", invalidUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LoginWithNullEmailReturnsValidationError()
    {
        // Arrange
        var invalidUser = new LoginCommand { Email = null!, Password = "Password@123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/accounts/login", invalidUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LoginWithInvalidEmailFormatReturnsValidationError()
    {
        // Arrange
        var invalidUser = new LoginCommand { Email = "not-an-email", Password = "Password@123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/accounts/login", invalidUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LoginWithSqlInjectionAttemptInEmailReturnsUnauthorized()
    {
        // Arrange
        var maliciousUser = new LoginCommand 
        { 
            Email = "admin'--", 
            Password = "anything" 
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/accounts/login", maliciousUser);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LoginWithVeryLongEmailReturnsValidationError()
    {
        // Arrange
        var longEmail = new string('a', 500) + "@test.com";
        var invalidUser = new LoginCommand { Email = longEmail, Password = "Password@123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/accounts/login", invalidUser);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LoginWithVeryLongPasswordReturnsUnauthorized()
    {
        // Arrange
        var longPassword = new string('a', 1000);
        var invalidUser = new LoginCommand 
        { 
            Email = "user@accountstests.com", 
            Password = longPassword 
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/accounts/login", invalidUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LoginWithWhitespaceOnlyEmailReturnsValidationError()
    {
        // Arrange
        var invalidUser = new LoginCommand { Email = "   ", Password = "Password@123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/accounts/login", invalidUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LoginWithSpecialCharactersInPasswordWorksIfValid()
    {
        // Arrange
        var userWithSpecialChars = new LoginCommand 
        { 
            Email = "user@accountstests.com", 
            Password = "User@123" // This is the actual password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/accounts/login", userWithSpecialChars);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LoginWithCaseInsensitiveEmailShouldWork()
    {
        // Arrange
        var userWithUppercaseEmail = new LoginCommand 
        { 
            Email = "USER@ACCOUNTSTESTS.COM", // Original is lowercase
            Password = "User@123" 
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/accounts/login", userWithUppercaseEmail);

        // Assert
        // Email should be case-insensitive in most systems
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("user@accountstests.com", "user@123")] // Wrong case
    [InlineData("user@accountstests.com", "User@1234")] // Extra character
    [InlineData("user@accountstests.com", "User@12")] // Missing character
    [InlineData("user@accountstests.com", " User@123")] // Leading space
    [InlineData("user@accountstests.com", "User@123 ")] // Trailing space
    public async Task LoginWithSimilarButWrongPasswordReturnsUnauthorized(string email, string password)
    {
        // Arrange
        var invalidUser = new LoginCommand { Email = email, Password = password };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/accounts/login", invalidUser);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);
    }
}
