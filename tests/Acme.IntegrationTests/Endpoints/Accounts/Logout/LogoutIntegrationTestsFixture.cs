using Acme.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Acme.IntegrationTests.Endpoints.Accounts.Logout;

/// <summary>
/// Test fixture for Logout integration tests.
/// </summary>
public class LogoutIntegrationTestsFixture : IntegrationTestBase
{
    public LogoutIntegrationTestsFixture() : base("LogoutIntegrationTests")
    {
    }

    protected override async Task SeedTestData(UserManager<Account> userManager, RoleManager<IdentityRole> roleManager)
    {
        // Create roles
        await roleManager.CreateAsync(new IdentityRole("Admin"));
        await roleManager.CreateAsync(new IdentityRole("User"));

        // Create admin user
        var (adminAccount, adminPassword, adminRole) = LogoutIntegrationTestsData.GetAdminAccount();
        await userManager.CreateAsync(adminAccount, adminPassword);
        await userManager.AddToRoleAsync(adminAccount, adminRole);

        // Create regular user
        var (userAccount, userPassword, userRole) = LogoutIntegrationTestsData.GetRegularAccount();
        await userManager.CreateAsync(userAccount, userPassword);
        await userManager.AddToRoleAsync(userAccount, userRole);
    }
}
