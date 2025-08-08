// -----------------------------------------------------------------------
// <copyright file="UpdateCommand.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MediatR;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Shared.Application.Commands;

/// <summary>
/// Abstract base record for update commands in the CQRS pattern.
/// Represents a command that modifies an existing entity identified by its ID.
/// This serves as a foundation for all update operations across the application modules.
/// </summary>
/// <param name="Id">The unique identifier of the entity to update.</param>
/// <remarks>
/// Update commands follow the Command pattern and are processed by MediatR.
/// They return a Result indicating success or failure of the update operation.
/// Concrete implementations should include all necessary data for entity modification.
/// </remarks>
public abstract record UpdateCommand(Guid Id) : IRequest<Result>;
