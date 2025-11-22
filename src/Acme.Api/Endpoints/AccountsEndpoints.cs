using Acme.Api.Extensions;
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
        var accounts = app.MapGroup("api/v1/accounts").RequireAuthorization();

        accounts.MapPost("/", async (ISender sender, [FromBody] RegisterAccountCommand registerAccountCommand, CancellationToken cancellationToken) =>(await sender.Send(registerAccountCommand, cancellationToken)).ToCreatedResult("/api/v1/accounts")).RequireAuthorization("AdminOnly");
    }
}
