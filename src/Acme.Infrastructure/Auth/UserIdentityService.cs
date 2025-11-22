// -----------------------------------------------------------------------
// <copyright file="UserIdentityService.cs" company="Acme">
// Copyright (c) Acme. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Acme.Infrastructure.Auth;

using Acme.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

public class UserIdentityService(IHttpContextAccessor httpContextAccessor) : IUserIdentityService
{
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;

    public string? GetUserId()
    {
        return httpContextAccessor.HttpContext?.User?.FindFirstValue("");
    }
}
