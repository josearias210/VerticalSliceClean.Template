using System.Net;
using FluentAssertions;
using Acme.Application.Features.Account.Login;
using Acme.IntegrationTests.Helpers;
using Xunit;

namespace Acme.IntegrationTests.Endpoints.Accounts.RegisterAccount;

/// <summary>
/// Integration tests for RegisterAccount endpoint.
/// </summary>
public class RegisterAccountIntegrationTests : IClassFixture<RegisterAccountIntegrationTestsFixture>
{
    private readonly HttpClient client;

    public RegisterAccountIntegrationTests(RegisterAccountIntegrationTestsFixture fixture)
    {
        client = fixture.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task RegisterAccountAsAdminCreatesNewUser()
    {
        // Arrange
        var adminUser = RegisterAccountIntegrationTestsData.GetAdminUser();
        var adminToken = await TestAuthenticationHelper.LoginAndGetTokenAsync(client, adminUser);
        var newUser = RegisterAccountIntegrationTestsData.GetNewUserToRegister();

        var request = TestAuthenticationHelper.CreateAuthenticatedRequest(
            HttpMethod.Post, 
            "/api/v1/accounts", 
            adminToken);
        request.Content = JsonContent.Create(newUser);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, $"Response: {content}");
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterAccountAsRegularUserReturnsForbidden()
    {
        // Arrange
        var regularUser = RegisterAccountIntegrationTestsData.GetRegularUser();
        var userToken = await TestAuthenticationHelper.LoginAndGetTokenAsync(client, regularUser);
        var newUser = RegisterAccountIntegrationTestsData.GetNewUserToRegister();

        var request = TestAuthenticationHelper.CreateAuthenticatedRequest(
            HttpMethod.Post, 
            "/api/v1/accounts", 
            userToken);
        request.Content = JsonContent.Create(newUser);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
