using Acme.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Acme.IntegrationTests.Endpoints.Accounts.GetProfile;

/// <summary>
/// Test fixture for GetProfile integration tests.
/// </summary>
public class GetProfileIntegrationTestsFixture : IntegrationTestBase
{
    public GetProfileIntegrationTestsFixture() : base("GetProfileIntegrationTests")
    {
    }

    protected override async Task SeedTestData(UserManager<Account> userManager, RoleManager<IdentityRole> roleManager)
    {
        // Create roles
        await roleManager.CreateAsync(new IdentityRole("Admin"));
        await roleManager.CreateAsync(new IdentityRole("User"));

        // Create admin user
        var (adminAccount, adminPassword, adminRole) = GetProfileIntegrationTestsData.GetAdminAccount();
        await userManager.CreateAsync(adminAccount, adminPassword);
        await userManager.AddToRoleAsync(adminAccount, adminRole);

        // Create regular user
        var (userAccount, userPassword, userRole) = GetProfileIntegrationTestsData.GetRegularAccount();
        await userManager.CreateAsync(userAccount, userPassword);
        await userManager.AddToRoleAsync(userAccount, userRole);
    }
}
