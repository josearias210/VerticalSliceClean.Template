using Acme.Application.Features.Account.Login;
using Acme.Domain.Entities;

namespace Acme.IntegrationTests.Endpoints.Accounts.GetProfile;

/// <summary>
/// Test data provider for GetProfile integration tests.
/// </summary>
public static class GetProfileIntegrationTestsData
{
    public static LoginCommand GetRegularUser() => new()
    {
        Email = "user@getprofiletests.com",
        Password = "User@123"
    };

    public static LoginCommand GetAdminUser() => new()
    {
        Email = "admin@getprofiletests.com",
        Password = "Admin@123"
    };

    // Account objects for seeding
    public static (Account Account, string Password, string Role) GetAdminAccount() => 
        (new Account
        {
            UserName = "admin@getprofiletests.com",
            Email = "admin@getprofiletests.com",
            EmailConfirmed = true,
            LockoutEnabled = false
        }, "Admin@123", "Admin");

    public static (Account Account, string Password, string Role) GetRegularAccount() => 
        (new Account
        {
            UserName = "user@getprofiletests.com",
            Email = "user@getprofiletests.com",
            EmailConfirmed = true,
            LockoutEnabled = false
        }, "User@123", "User");
}
