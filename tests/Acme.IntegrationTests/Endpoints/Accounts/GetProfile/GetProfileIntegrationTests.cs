using System.Net;
using FluentAssertions;
using Acme.Application.Features.Account.GetProfile;
using Acme.Application.Features.Account.Login;
using Acme.IntegrationTests.Helpers;
using Xunit;

namespace Acme.IntegrationTests.Endpoints.Accounts.GetProfile;

/// <summary>
/// Integration tests for GetProfile endpoint.
/// </summary>
public class GetProfileIntegrationTests : IClassFixture<GetProfileIntegrationTestsFixture>
{
    private readonly HttpClient client;
    private readonly GetProfileIntegrationTestsFixture fixture;

    public GetProfileIntegrationTests(GetProfileIntegrationTestsFixture fixture)
    {
        this.fixture = fixture;
        client = fixture.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetProfileWithValidTokenReturnsUserProfile()
    {
        // Arrange
        var regularUser = GetProfileIntegrationTestsData.GetRegularUser();
        var accessToken = await TestAuthenticationHelper.LoginAndGetTokenAsync(client, regularUser);
        var request = TestAuthenticationHelper.CreateAuthenticatedRequest(HttpMethod.Get, "/api/v1/accounts/me", accessToken);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var profile = await response.Content.ReadFromJsonAsync<GetProfileQueryResponse>();
        profile.Should().NotBeNull();
        profile!.Email.Should().Be(regularUser.Email);
    }

    [Fact]
    public async Task GetProfileWithoutTokenReturnsUnauthorized()
    {
        // Act
        var response = await client.GetAsync("/api/v1/accounts/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
