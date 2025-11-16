using Acme.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Acme.IntegrationTests.Endpoints.Accounts.RegisterAccount;

/// <summary>
/// Test fixture for RegisterAccount integration tests.
/// </summary>
public class RegisterAccountIntegrationTestsFixture : IntegrationTestBase
{
    public RegisterAccountIntegrationTestsFixture() : base("RegisterAccountIntegrationTests")
    {
    }

    protected override async Task SeedTestData(UserManager<Account> userManager, RoleManager<IdentityRole> roleManager)
    {
        // Create roles
        await roleManager.CreateAsync(new IdentityRole("Admin"));
        await roleManager.CreateAsync(new IdentityRole("User"));

        // Create admin user
        var (adminAccount, adminPassword, adminRole) = RegisterAccountIntegrationTestsData.GetAdminAccount();
        await userManager.CreateAsync(adminAccount, adminPassword);
        await userManager.AddToRoleAsync(adminAccount, adminRole);

        // Create regular user
        var (userAccount, userPassword, userRole) = RegisterAccountIntegrationTestsData.GetRegularAccount();
        await userManager.CreateAsync(userAccount, userPassword);
        await userManager.AddToRoleAsync(userAccount, userRole);
    }
}
