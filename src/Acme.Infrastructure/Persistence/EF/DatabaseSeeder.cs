using Acme.Application.Abstractions;
using Acme.Domain.Entities;
using Acme.Domain.Enums;
using Acme.Infrastructure.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Acme.Infrastructure.Persistence.EF;

public class DatabaseSeeder(
    RoleManager<IdentityRole> roleManager,
    UserManager<Account> userManager,
    ApplicationDbContext dbContext,
    IOptions<AdminUserSettings> adminUserOptions,
    ILogger<DatabaseSeeder> logger) : IDatabaseSeeder
{
    private readonly RoleManager<IdentityRole> roleManager = roleManager;
    private readonly UserManager<Account> userManager = userManager;
    private readonly ApplicationDbContext dbContext = dbContext;
    private readonly AdminUserSettings adminUserSettings = adminUserOptions.Value;
    private readonly ILogger<DatabaseSeeder> logger = logger;

    public async Task SeedRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = Enum.GetValues<Role>();

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role.ToString()))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role.ToString()));
                if (result.Succeeded)
                {
                    logger.LogInformation("Created role '{Role}'", role);
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => $"{e.Code}:{e.Description}"));
                    logger.LogWarning("Failed to create role '{Role}': {Errors}", role, errors);
                }
            }
        }
    }

    public async Task SeedAdminUserAsync(CancellationToken cancellationToken = default)
    {
        var email = adminUserSettings.Email;
        var password = adminUserSettings.Password;
        const string adminRole = nameof(Role.Developer);

        // Validate configuration
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Admin user not created. Missing Admin:Email or Admin:Password configuration.");
            return;
        }

        // Validate password strength (minimum 12 characters for admin)
        if (password.Length < 12)
        {
            logger.LogError("Admin password must be at least 12 characters. Admin user not created for security reasons.");
            return;
        }

        var hasUpperCase = password.Any(char.IsUpper);
        var hasLowerCase = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecialChar = password.Any(c => !char.IsLetterOrDigit(c));

        if (!hasUpperCase || !hasLowerCase || !hasDigit || !hasSpecialChar)
        {
            logger.LogError("Admin password must contain at least one uppercase, lowercase, digit, and special character. Admin user not created for security reasons.");
            return;
        }

        var existing = await userManager.FindByEmailAsync(email);
        if (existing != null)
        {
            logger.LogInformation("Admin user '{Email}' already exists", email);

            // Ensure user has Admin role
            if (!await userManager.IsInRoleAsync(existing, adminRole))
            {
                var roleResult = await userManager.AddToRoleAsync(existing, adminRole);
                if (roleResult.Succeeded)
                {
                    logger.LogInformation("Assigned role '{Role}' to user '{Email}'", adminRole, email);
                }
                else
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => $"{e.Code}:{e.Description}"));
                    logger.LogWarning("Failed to assign role '{Role}' to user '{Email}': {Errors}", adminRole, email, errors);
                }
            }
            return;
        }

        // NOTE: We can't use manual transactions when ExecutionStrategy is configured with retry logic
        // Instead, UserManager operations are atomic by design and handle their own transactions
        var user = new Account
        {
            Email = email,
            UserName = email,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => $"{e.Code}:{e.Description}"));
            logger.LogWarning("Failed to create admin user '{Email}': {Errors}", email, errors);
            return;
        }

        var addRoleResult = await userManager.AddToRoleAsync(user, adminRole);
        if (!addRoleResult.Succeeded)
        {
            var errors = string.Join(", ", addRoleResult.Errors.Select(e => $"{e.Code}:{e.Description}"));
            logger.LogError("Failed to assign role '{Role}' to admin user '{Email}': {Errors}", adminRole, email, errors);
            
            // Cleanup: delete the user since role assignment failed
            await userManager.DeleteAsync(user);
            logger.LogWarning("Rolled back: Deleted user '{Email}' because role assignment failed", email);
            return;
        }

        logger.LogInformation("Successfully created admin user '{Email}' with role '{Role}'", email, adminRole);
    }
}
