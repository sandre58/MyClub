// -----------------------------------------------------------------------
// <copyright file="CreateCommand.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MediatR;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Shared.Application.Commands;

public abstract record CreateCommand : IRequest<Result<Guid>>;
