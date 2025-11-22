using System.Security.Claims;
using Acme.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Acme.Infrastructure.Auth.OpenIddict;

public class PasswordGrantHandler : IOpenIddictServerHandler<HandleTokenRequestContext>
{
    private readonly UserManager<Account> _userManager;
    private readonly SignInManager<Account> _signInManager;

    public PasswordGrantHandler(
        UserManager<Account> userManager,
        SignInManager<Account> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async ValueTask HandleAsync(HandleTokenRequestContext context)
    {
        if (!context.Request.IsPasswordGrantType())
        {
            return;
        }

        var user = await _userManager.FindByNameAsync(context.Request.Username!) ??
                   await _userManager.FindByEmailAsync(context.Request.Username!);

        if (user == null)
        {
            context.Reject(
                error: OpenIddictConstants.Errors.InvalidGrant,
                description: "The username/password couple is invalid.");
            return;
        }

        // Validate the password
        var result = await _signInManager.CheckPasswordSignInAsync(user, context.Request.Password!, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            context.Reject(
                error: OpenIddictConstants.Errors.InvalidGrant,
                description: "The username/password couple is invalid.");
            return;
        }

        // Create the principal
        var principal = await _signInManager.CreateUserPrincipalAsync(user);

        // Set the list of scopes granted to the client application.
        principal.SetScopes(context.Request.GetScopes());

        // Ensure the 'sub' claim is present
        var identity = (ClaimsIdentity)principal.Identity!;
        if (!principal.HasClaim(c => c.Type == OpenIddictConstants.Claims.Subject))
        {
            var userId = await _userManager.GetUserIdAsync(user);
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, userId));
        }

        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, principal));
        }

        context.SignIn(principal);
    }

    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
    {
        switch (claim.Type)
        {
            case OpenIddictConstants.Claims.Name:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (principal.HasScope(OpenIddictConstants.Scopes.Profile))
                    yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;

            case OpenIddictConstants.Claims.Email:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (principal.HasScope(OpenIddictConstants.Scopes.Email))
                    yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;

            case OpenIddictConstants.Claims.Role:
                yield return OpenIddictConstants.Destinations.AccessToken;

                if (principal.HasScope(OpenIddictConstants.Scopes.Roles))
                    yield return OpenIddictConstants.Destinations.IdentityToken;

                yield break;

            case OpenIddictConstants.Claims.Subject:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield return OpenIddictConstants.Destinations.IdentityToken;
                yield break;

            case "AspNet.Identity.SecurityStamp": yield break;

            default:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;
        }
    }
}
