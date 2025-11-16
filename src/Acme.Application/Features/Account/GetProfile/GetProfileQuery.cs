// -----------------------------------------------------------------------
// <copyright file="GetProfileResponse.cs" company="Acme">
// Copyright (c) Acme. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using ErrorOr;
using MediatR;

namespace Acme.Application.Features.Account.GetProfile;

public class GetProfileQuery : IRequest<ErrorOr<GetProfileQueryResponse>>
{
}
