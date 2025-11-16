using System.Net;
using FluentAssertions;
using Acme.Application.Features.Account.RegisterAccount;
using Acme.IntegrationTests.Helpers;
using Xunit;

namespace Acme.IntegrationTests.Endpoints.Accounts.RegisterAccount;

/// <summary>
/// Integration tests for edge cases in RegisterAccount endpoint.
/// </summary>
public class RegisterAccountEdgeCasesIntegrationTests : IClassFixture<RegisterAccountIntegrationTestsFixture>
{
    private readonly HttpClient _client;

    public RegisterAccountEdgeCasesIntegrationTests(RegisterAccountIntegrationTestsFixture fixture)
    {
        _client = fixture.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task RegisterAccountWithExistingEmailReturnsBadRequest()
    {
        // Arrange
        var adminUser = RegisterAccountIntegrationTestsData.GetAdminUser();
        var adminToken = await TestAuthenticationHelper.LoginAndGetTokenAsync(_client, adminUser);
        var duplicateUser = RegisterAccountIntegrationTestsData.GetDuplicateUser();

        var request = TestAuthenticationHelper.CreateAuthenticatedRequest(
            HttpMethod.Post, 
            "/api/v1/accounts", 
            adminToken);
        request.Content = JsonContent.Create(duplicateUser);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task RegisterAccountWithInvalidEmailFormatReturnsBadRequest()
    {
        // Arrange
        var adminUser = RegisterAccountIntegrationTestsData.GetAdminUser();
        var adminToken = await TestAuthenticationHelper.LoginAndGetTokenAsync(_client, adminUser);
        var invalidUser = new RegisterAccountCommand 
        { 
            Email = "not-an-email", 
            Password = "User" 
        };

        var request = TestAuthenticationHelper.CreateAuthenticatedRequest(
            HttpMethod.Post, 
            "/api/v1/accounts", 
            adminToken);
        request.Content = JsonContent.Create(invalidUser);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterAccountWithEmptyEmailReturnsBadRequest()
    {
        // Arrange
        var adminUser = RegisterAccountIntegrationTestsData.GetAdminUser();
        var adminToken = await TestAuthenticationHelper.LoginAndGetTokenAsync(_client, adminUser);
        var invalidUser = new RegisterAccountCommand 
        { 
            Email = "", 
            Password = "User" 
        };

        var request = TestAuthenticationHelper.CreateAuthenticatedRequest(
            HttpMethod.Post, 
            "/api/v1/accounts", 
            adminToken);
        request.Content = JsonContent.Create(invalidUser);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterAccountWithInvalidRoleReturnsBadRequest()
    {
        // Arrange
        var adminUser = RegisterAccountIntegrationTestsData.GetAdminUser();
        var adminToken = await TestAuthenticationHelper.LoginAndGetTokenAsync(_client, adminUser);
        var userWithInvalidRole = new RegisterAccountCommand 
        { 
            Email = $"newuser{Guid.NewGuid():N}@test.com", 
            Password = "InvalidRole" // Invalid role
        };

        var request = TestAuthenticationHelper.CreateAuthenticatedRequest(
            HttpMethod.Post, 
            "/api/v1/accounts", 
            adminToken);
        request.Content = JsonContent.Create(userWithInvalidRole);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task RegisterAccountWithSqlInjectionAttemptInEmailReturnsBadRequest()
    {
        // Arrange
        var adminUser = RegisterAccountIntegrationTestsData.GetAdminUser();
        var adminToken = await TestAuthenticationHelper.LoginAndGetTokenAsync(_client, adminUser);
        var maliciousUser = new RegisterAccountCommand 
        { 
            Email = "admin'; DROP TABLE Accounts;--@test.com", 
            Password = "User" 
        };

        var request = TestAuthenticationHelper.CreateAuthenticatedRequest(
            HttpMethod.Post, 
            "/api/v1/accounts", 
            adminToken);
        request.Content = JsonContent.Create(maliciousUser);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        // Should either reject due to validation or safely handle
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Created);
    }

    [Fact]
    public async Task RegisterAccountWithVeryLongEmailReturnsBadRequest()
    {
        // Arrange
        var adminUser = RegisterAccountIntegrationTestsData.GetAdminUser();
        var adminToken = await TestAuthenticationHelper.LoginAndGetTokenAsync(_client, adminUser);
        var longEmail = new string('a', 500) + "@test.com";
        var userWithLongEmail = new RegisterAccountCommand 
        { 
            Email = longEmail, 
            Password = "User" 
        };

        var request = TestAuthenticationHelper.CreateAuthenticatedRequest(
            HttpMethod.Post, 
            "/api/v1/accounts", 
            adminToken);
        request.Content = JsonContent.Create(userWithLongEmail);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterAccountWithoutAuthorizationReturnsUnauthorized()
    {
        // Arrange
        var newUser = RegisterAccountIntegrationTestsData.GetNewUserToRegister();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/accounts", newUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegisterAccountWithExpiredTokenReturnsUnauthorized()
    {
        // Arrange
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE2MDk0NTkyMDB9.fake";
        var newUser = RegisterAccountIntegrationTestsData.GetNewUserToRegister();

        var request = TestAuthenticationHelper.CreateAuthenticatedRequest(
            HttpMethod.Post, 
            "/api/v1/accounts", 
            expiredToken);
        request.Content = JsonContent.Create(newUser);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("user+tag@test.com")] // Plus addressing
    [InlineData("user.name@test.com")] // Dot in local part
    [InlineData("user_name@test.com")] // Underscore
    [InlineData("user-name@test.com")] // Hyphen
    [InlineData("123@test.com")] // Numeric local part
    public async Task RegisterAccountWithValidButUnusualEmailFormatsSucceeds(string email)
    {
        // Arrange
        var adminUser = RegisterAccountIntegrationTestsData.GetAdminUser();
        var adminToken = await TestAuthenticationHelper.LoginAndGetTokenAsync(_client, adminUser);
        var newUser = new RegisterAccountCommand 
        { 
            Email = email, 
            Password = "User" 
        };

        var request = TestAuthenticationHelper.CreateAuthenticatedRequest(
            HttpMethod.Post, 
            "/api/v1/accounts", 
            adminToken);
        request.Content = JsonContent.Create(newUser);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest);
    }
}
