// -----------------------------------------------------------------------
// <copyright file="GetProfileResponse.cs" company="Acme">
// Copyright (c) Acme. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Acme.Application.Features.Account.GetProfile;

public class GetProfileQueryResponse
{
    public required string UserId { get; set; }
    public required string Email { get; set; }
}
