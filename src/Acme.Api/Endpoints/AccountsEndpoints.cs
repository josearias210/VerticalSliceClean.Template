using Acme.Api.Extensions;
using Acme.Application.Features.Account.GetProfile;
using Acme.Application.Features.Account.Login;
using Acme.Application.Features.Account.Logout;
using Acme.Application.Features.Account.RefreshToken;
using Acme.Application.Features.Account.RegisterAccount;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Acme.Api.Endpoints;

public sealed class AccountsEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var accounts = app.MapGroup("account")
            .RequireAuthorization();

        accounts.MapPost("login", async (ISender sender, [FromBody] LoginCommand loginCommand, CancellationToken cancellationToken) =>
            (await sender.Send(loginCommand, cancellationToken)).ToAuthResult())
        .WithAnonymousAuth()
        .WithMetadata(
            "Login",
            "Authenticate user and generate tokens",
            "Authenticates a user with email and password. Returns user profile and sets access/refresh tokens in httpOnly cookies.");

        accounts.MapPost("refresh", async (ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new RefreshTokenCommand(), cancellationToken)).ToAuthResult())
        .WithAnonymousAuth()
        .WithMetadata(
            "RefreshToken",
            "Refresh access token using refresh token from httpOnly cookie",
            "Generates a new access token using the refresh token from httpOnly cookie. Returns new user profile and updates tokens in cookies.");

        accounts.MapPost("logout", async (ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new LogoutCommand(), cancellationToken)).ToNoContentResult())
        .WithMetadata(
            "Logout",
            "Logout and clear authentication cookies",
            "Revokes the current refresh token and clears all authentication cookies. Requires valid access token.");

        accounts.MapGet("me", async (ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new GetProfileQuery(), cancellationToken)).ToTypedResult())
        .WithMetadata(
            "GetProfile",
            "Get current user profile",
            "Returns the profile of the currently authenticated user. Requires valid JWT token in Authorization header.");

        accounts.MapPost("/", async (ISender sender, [FromBody] RegisterAccountCommand registerAccountCommand, CancellationToken cancellationToken) =>
            (await sender.Send(registerAccountCommand, cancellationToken)).ToCreatedResult("/api/v1/accounts"))
        .RequireAuthorization("AdminOnly")
        .WithMetadata(
            "RegisterAccount",
            "Register new user account (Admin only)",
            "Creates a new user account with auto-generated password. Only admins can register new accounts. " +
            "Password is randomly generated (16 chars with complexity) and sent via email. " +
            "Request body: { \"email\": \"user@example.com\", \"password\": \"User\" } where password field specifies the role (Admin, User, Manager, ProductManager).");
    }
}
