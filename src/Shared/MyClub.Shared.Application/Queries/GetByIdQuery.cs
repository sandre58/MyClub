// -----------------------------------------------------------------------
// <copyright file="GetByIdQuery.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MediatR;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Shared.Application.Queries;

/// <summary>
/// Abstract base record for queries that retrieve a single entity by its identifier.
/// Represents a query that returns an entity of type TResponse identified by a GUID.
/// This serves as a foundation for all get-by-id operations across the application modules.
/// </summary>
/// <typeparam name="TResponse">The type of entity being retrieved.</typeparam>
/// <param name="Id">The unique identifier of the entity to retrieve.</param>
/// <remarks>
/// Get-by-ID queries follow the Query pattern and are processed by MediatR.
/// They return a Result&lt;TResponse&gt; containing either the requested entity or error information.
/// This pattern ensures consistent entity retrieval across all modules.
/// </remarks>
public abstract record GetByIdQuery<TResponse>(Guid Id) : IRequest<Result<TResponse>>;
