using System.Net;
using FluentAssertions;
using Acme.Application.Features.Account.Login;
using Acme.IntegrationTests.Helpers;
using Xunit;

namespace Acme.IntegrationTests.Endpoints.Accounts.Login;

/// <summary>
/// Integration tests for Login endpoint.
/// </summary>
public class LoginIntegrationTests : IClassFixture<LoginIntegrationTestsFixture>
{
    private readonly HttpClient client;

    public LoginIntegrationTests(LoginIntegrationTestsFixture fixture)
    {
        client = fixture.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task LoginWithValidAdminCredentialsReturnsOkAndSetsTokens()
    {
        // Arrange
        var adminUser = LoginIntegrationTestsData.GetAdminUser();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/accounts/login", adminUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<LoginCommandResponse>();
        result.Should().NotBeNull();
        result!.Email.Should().Be(adminUser.Email);
        result.Roles.Should().Contain("Admin");
        
        // Validate authentication cookies
        TestAuthenticationHelper.ValidateAuthCookies(response);
    }

    [Fact]
    public async Task LoginWithValidUserCredentialsReturnsOkAndSetsTokens()
    {
        // Arrange
        var regularUser = LoginIntegrationTestsData.GetRegularUser();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/accounts/login", regularUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<LoginCommandResponse>();
        result.Should().NotBeNull();
        result!.Email.Should().Be(regularUser.Email);
        result.Roles.Should().Contain("User");
        
        // Validate authentication cookies
        TestAuthenticationHelper.ValidateAuthCookies(response);
    }

    [Fact]
    public async Task LoginWithWrongPasswordReturnsUnauthorized()
    {
        // Arrange
        var userWithWrongPassword = LoginIntegrationTestsData.GetUserWithWrongPassword();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/accounts/login", userWithWrongPassword);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LoginWithNonExistentUserReturnsUnauthorized()
    {
        // Arrange
        var nonExistentUser = LoginIntegrationTestsData.GetNonExistentUser();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/accounts/login", nonExistentUser);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
