// -----------------------------------------------------------------------
// <copyright file="DeleteCommand.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MediatR;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Shared.Application.Commands;

/// <summary>
/// Abstract base record for delete commands in the CQRS pattern.
/// Represents a command that removes an existing entity identified by its ID.
/// This serves as a foundation for all delete operations across the application modules.
/// </summary>
/// <param name="Id">The unique identifier of the entity to delete.</param>
/// <remarks>
/// Delete commands follow the Command pattern and are processed by MediatR.
/// They return a Result indicating success or failure of the delete operation.
/// The actual deletion strategy (hard delete, soft delete, etc.) is implemented in the concrete handler.
/// </remarks>
public abstract record DeleteCommand(Guid Id) : IRequest<Result>;
