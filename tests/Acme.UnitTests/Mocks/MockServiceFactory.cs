using Acme.Application.Abstractions;
using Moq;

namespace Acme.UnitTests.Mocks;

/// <summary>
/// Factory for creating mock service instances for testing.
/// </summary>
public static class MockServiceFactory
{
    /// <summary>
    /// Creates a mock UserIdentityService that returns the specified user ID.
    /// </summary>
    /// <param name="userId">The user ID to return from GetUserId().</param>
    /// <returns>A configured Mock of IUserIdentityService.</returns>
    public static Mock<IUserIdentityService> CreateUserIdentityService(string userId)
    {
        var mock = new Mock<IUserIdentityService>();
        mock.Setup(x => x.GetUserId()).Returns(userId);
        return mock;
    }

    /// <summary>
    /// Creates a mock TokenService.
    /// </summary>
    /// <returns>A configured Mock of ITokenService.</returns>
    public static Mock<ITokenService> CreateTokenService()
    {
        return new Mock<ITokenService>();
    }

    /// <summary>
    /// Creates a mock CookieTokenService.
    /// </summary>
    /// <returns>A configured Mock of ICookieTokenService.</returns>
    public static Mock<ICookieTokenService> CreateCookieTokenService()
    {
        return new Mock<ICookieTokenService>();
    }

    /// <summary>
    /// Creates a mock EmailService.
    /// </summary>
    /// <returns>A configured Mock of IEmailService.</returns>
    public static Mock<IEmailService> CreateEmailService()
    {
        return new Mock<IEmailService>();
    }
}
