// -----------------------------------------------------------------------
// <copyright file="LogoutCommand.cs" company="Acme">
// Copyright (c) Acme. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using ErrorOr;
using MediatR;

namespace Acme.Application.Features.Account.Logout;

public class LogoutCommand : IRequest<ErrorOr<bool>>
{
}
