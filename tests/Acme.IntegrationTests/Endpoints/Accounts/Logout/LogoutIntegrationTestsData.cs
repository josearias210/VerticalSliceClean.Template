using Acme.Application.Features.Account.Login;
using Acme.Domain.Entities;

namespace Acme.IntegrationTests.Endpoints.Accounts.Logout;

/// <summary>
/// Test data provider for Logout integration tests.
/// </summary>
public static class LogoutIntegrationTestsData
{
    public static LoginCommand GetRegularUser() => new()
    {
        Email = "user@logouttests.com",
        Password = "User@123"
    };

    public static LoginCommand GetAdminUser() => new()
    {
        Email = "admin@logouttests.com",
        Password = "Admin@123"
    };

    // Account objects for seeding
    public static (Account Account, string Password, string Role) GetAdminAccount() => 
        (new Account
        {
            UserName = "admin@logouttests.com",
            Email = "admin@logouttests.com",
            EmailConfirmed = true,
            LockoutEnabled = false
        }, "Admin@123", "Admin");

    public static (Account Account, string Password, string Role) GetRegularAccount() => 
        (new Account
        {
            UserName = "user@logouttests.com",
            Email = "user@logouttests.com",
            EmailConfirmed = true,
            LockoutEnabled = false
        }, "User@123", "User");
}
