using Acme.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Acme.IntegrationTests.Endpoints.Accounts.Login;

/// <summary>
/// Test fixture for Login integration tests.
/// Creates a dedicated LocalDB database and seeds initial test data.
/// </summary>
public class LoginIntegrationTestsFixture : IntegrationTestBase
{
    public LoginIntegrationTestsFixture() : base("LoginIntegrationTests")
    {
    }

    protected override async Task SeedTestData(UserManager<Account> userManager, RoleManager<IdentityRole> roleManager)
    {
        // Create roles
        await roleManager.CreateAsync(new IdentityRole("Admin"));
        await roleManager.CreateAsync(new IdentityRole("User"));

        // Create admin user
        var (adminAccount, adminPassword, adminRole) = LoginIntegrationTestsData.GetAdminAccount();
        await userManager.CreateAsync(adminAccount, adminPassword);
        await userManager.AddToRoleAsync(adminAccount, adminRole);

        // Create regular user
        var (userAccount, userPassword, userRole) = LoginIntegrationTestsData.GetRegularAccount();
        await userManager.CreateAsync(userAccount, userPassword);
        await userManager.AddToRoleAsync(userAccount, userRole);
    }
}
