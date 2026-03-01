namespace Acme.Api.Endpoints;

using Acme.Api.Extensions;
using Acme.Application.Features.Account.RegisterAccount;
using Acme.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

public sealed class AccountsEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var accounts = app.MapGroup("api/v1/accounts")
            .RequireAuthorization()
            .RequireRateLimiting("auth");

        accounts.MapPost("/", async (ISender sender, [FromBody] RegisterAccountCommand command, CancellationToken cancellationToken) => (await sender.Send(command, cancellationToken)).ToTypedResult())
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));
    }
}
