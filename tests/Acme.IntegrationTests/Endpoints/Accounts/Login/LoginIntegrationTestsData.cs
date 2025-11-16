using Acme.Application.Features.Account.Login;
using Acme.Domain.Entities;

namespace Acme.IntegrationTests.Endpoints.Accounts.Login;

/// <summary>
/// Test data provider for Login integration tests.
/// </summary>
public static class LoginIntegrationTestsData
{
    // Admin user credentials
    public static LoginCommand GetAdminUser() => new()
    {
        Email = "admin@accountstests.com",
        Password = "Admin@123"
    };

    // Regular user credentials
    public static LoginCommand GetRegularUser() => new()
    {
        Email = "user@accountstests.com",
        Password = "User@123"
    };

    // Account objects for seeding
    public static (Account Account, string Password, string Role) GetAdminAccount() => 
        (new Account
        {
            UserName = "admin@accountstests.com",
            Email = "admin@accountstests.com",
            EmailConfirmed = true,
            LockoutEnabled = false
        }, "Admin@123", "Admin");

    public static (Account Account, string Password, string Role) GetRegularAccount() => 
        (new Account
        {
            UserName = "user@accountstests.com",
            Email = "user@accountstests.com",
            EmailConfirmed = true,
            LockoutEnabled = false
        }, "User@123", "User");

    // Invalid credentials
    public static LoginCommand GetUserWithWrongPassword() => new()
    {
        Email = "user@accountstests.com",
        Password = "WrongPassword123"
    };

    public static LoginCommand GetNonExistentUser() => new()
    {
        Email = "nonexistent@accountstests.com",
        Password = "Password@123"
    };
}
