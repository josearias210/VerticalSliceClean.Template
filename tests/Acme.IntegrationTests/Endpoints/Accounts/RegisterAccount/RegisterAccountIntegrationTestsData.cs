using Acme.Application.Features.Account.Login;
using Acme.Application.Features.Account.RegisterAccount;
using Acme.Domain.Entities;

namespace Acme.IntegrationTests.Endpoints.Accounts.RegisterAccount;

/// <summary>
/// Test data provider for RegisterAccount integration tests.
/// </summary>
public static class RegisterAccountIntegrationTestsData
{
    public static LoginCommand GetAdminUser() => new()
    {
        Email = "admin@registeraccounttests.com",
        Password = "Admin@123"
    };

    public static LoginCommand GetRegularUser() => new()
    {
        Email = "user@registeraccounttests.com",
        Password = "User@123"
    };

    // Account objects for seeding
    public static (Account Account, string Password, string Role) GetAdminAccount() => 
        (new Account
        {
            UserName = "admin@registeraccounttests.com",
            Email = "admin@registeraccounttests.com",
            EmailConfirmed = true,
            LockoutEnabled = false
        }, "Admin@123", "Admin");

    public static (Account Account, string Password, string Role) GetRegularAccount() => 
        (new Account
        {
            UserName = "user@registeraccounttests.com",
            Email = "user@registeraccounttests.com",
            EmailConfirmed = true,
            LockoutEnabled = false
        }, "User@123", "User");

    public static RegisterAccountCommand GetNewUserToRegister() => new()
    {
        Email = $"newuser{Guid.NewGuid():N}@registeraccounttests.com",
        Password = "User" // Password field is repurposed as Role
    };

    public static RegisterAccountCommand GetNewAdminToRegister() => new()
    {
        Email = $"newadmin{Guid.NewGuid():N}@registeraccounttests.com",
        Password = "Admin" // Password field is repurposed as Role
    };

    public static RegisterAccountCommand GetDuplicateUser() => new()
    {
        Email = "user@registeraccounttests.com", // Already exists
        Password = "User" // Password field is repurposed as Role
    };
}
