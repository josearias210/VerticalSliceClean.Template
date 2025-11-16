using System.Net;
using FluentAssertions;
using Acme.Application.Features.Account.Login;
using Acme.IntegrationTests.Helpers;
using Xunit;

namespace Acme.IntegrationTests.Endpoints.Accounts.Logout;

/// <summary>
/// Integration tests for Logout endpoint.
/// </summary>
public class LogoutIntegrationTests : IClassFixture<LogoutIntegrationTestsFixture>
{
    private readonly HttpClient client;

    public LogoutIntegrationTests(LogoutIntegrationTestsFixture fixture)
    {
        client = fixture.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task LogoutWithValidTokenReturnsNoContent()
    {
        // Arrange
        var regularUser = LogoutIntegrationTestsData.GetRegularUser();
        var accessToken = await TestAuthenticationHelper.LoginAndGetTokenAsync(client, regularUser);

        // Act
        var response = await TestAuthenticationHelper.LogoutAsync(client, accessToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task LogoutWithoutTokenReturnsUnauthorized()
    {
        // Act
        var response = await client.PostAsync("/api/v1/accounts/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }


}
