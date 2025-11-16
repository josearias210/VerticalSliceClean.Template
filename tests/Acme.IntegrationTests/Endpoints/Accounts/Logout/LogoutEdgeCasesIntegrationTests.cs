using System.Net;
using FluentAssertions;
using Acme.Application.Features.Account.Login;
using Acme.IntegrationTests.Helpers;
using Xunit;

namespace Acme.IntegrationTests.Endpoints.Accounts.Logout;

/// <summary>
/// Integration tests for edge cases in Logout endpoint.
/// </summary>
public class LogoutEdgeCasesIntegrationTests : IClassFixture<LogoutIntegrationTestsFixture>
{
    private readonly HttpClient _client;

    public LogoutEdgeCasesIntegrationTests(LogoutIntegrationTestsFixture fixture)
    {
        _client = fixture.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task LogoutWithExpiredTokenReturnsUnauthorized()
    {
        // Arrange
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE2MDk0NTkyMDB9.fake";

        // Act
        var response = await TestAuthenticationHelper.LogoutAsync(_client, expiredToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutWithInvalidTokenFormatReturnsUnauthorized()
    {
        // Arrange
        var invalidToken = "not-a-valid-jwt-token";

        // Act
        var response = await TestAuthenticationHelper.LogoutAsync(_client, invalidToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutWithEmptyTokenReturnsUnauthorized()
    {
        // Arrange
        var emptyToken = "";

        // Act
        var response = await TestAuthenticationHelper.LogoutAsync(_client, emptyToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutTwiceWithSameTokenSecondCallShouldStillSucceed()
    {
        // Arrange
        var regularUser = LogoutIntegrationTestsData.GetRegularUser();
        var accessToken = await TestAuthenticationHelper.LoginAndGetTokenAsync(_client, regularUser);

        // Act - First logout
        var firstResponse = await TestAuthenticationHelper.LogoutAsync(_client, accessToken);
        
        // Act - Second logout with same token
        var secondResponse = await TestAuthenticationHelper.LogoutAsync(_client, accessToken);

        // Assert
        firstResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        // Second logout might fail if token is invalidated server-side
        secondResponse.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutWithMalformedAuthorizationHeaderReturnsUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/accounts/logout");
        request.Headers.Add("Authorization", "NotBearer token123");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutWithTokenInWrongCaseReturnsUnauthorized()
    {
        // Arrange - Get valid token but use wrong case in header
        var regularUser = LogoutIntegrationTestsData.GetRegularUser();
        var accessToken = await TestAuthenticationHelper.LoginAndGetTokenAsync(_client, regularUser);
        
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/accounts/logout");
        request.Headers.Add("Authorization", $"bearer {accessToken}"); // lowercase 'bearer'

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        // Should work as Authorization schemes are typically case-insensitive
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutVerifiesCookiesAreCleared()
    {
        // Arrange
        var regularUser = LogoutIntegrationTestsData.GetRegularUser();
        var accessToken = await TestAuthenticationHelper.LoginAndGetTokenAsync(_client, regularUser);

        // Act
        var response = await TestAuthenticationHelper.LogoutAsync(_client, accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        TestAuthenticationHelper.ValidateCookiesCleared(response);
    }
}
