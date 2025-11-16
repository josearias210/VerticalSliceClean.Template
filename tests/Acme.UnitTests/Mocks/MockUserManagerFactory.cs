using Acme.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Acme.UnitTests.Mocks;

/// <summary>
/// Factory for creating mock UserManager instances for testing.
/// </summary>
public static class MockUserManagerFactory
{
    /// <summary>
    /// Creates a mock UserManager with all required dependencies properly configured.
    /// </summary>
    /// <returns>A configured Mock of UserManager for Account entities.</returns>
    public static Mock<UserManager<Domain.Entities.Account>> Create()
    {
        var store = new Mock<IUserStore<Domain.Entities.Account>>();
        var options = new Mock<IOptions<IdentityOptions>>();
        var passwordHasher = new Mock<IPasswordHasher<Domain.Entities.Account>>();
        var userValidators = new List<IUserValidator<Domain.Entities.Account>>();
        var passwordValidators = new List<IPasswordValidator<Domain.Entities.Account>>();
        var keyNormalizer = new Mock<ILookupNormalizer>();
        var errors = new Mock<IdentityErrorDescriber>();
        var services = new Mock<IServiceProvider>();
        var logger = new Mock<ILogger<UserManager<Domain.Entities.Account>>>();

        return new Mock<UserManager<Domain.Entities.Account>>(
            store.Object,
            options.Object,
            passwordHasher.Object,
            userValidators,
            passwordValidators,
            keyNormalizer.Object,
            errors.Object,
            services.Object,
            logger.Object);
    }
}
