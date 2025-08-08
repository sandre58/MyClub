// -----------------------------------------------------------------------
// <copyright file="CreateCommand.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MediatR;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Shared.Application.Commands;

/// <summary>
/// Abstract base record for create commands in the CQRS pattern.
/// Represents a command that creates a new entity and returns the identifier of the created entity.
/// This serves as a foundation for all create operations across the application modules.
/// </summary>
/// <remarks>
/// Create commands follow the Command pattern and are processed by MediatR.
/// They return a Result&lt;Guid&gt; containing either the ID of the created entity or error information.
/// Concrete implementations should include all necessary data for entity creation.
/// </remarks>
public abstract record CreateCommand : IRequest<Result<Guid>>;
