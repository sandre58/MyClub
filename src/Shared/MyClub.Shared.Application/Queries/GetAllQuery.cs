// -----------------------------------------------------------------------
// <copyright file="GetAllQuery.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MediatR;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Shared.Application.Queries;

/// <summary>
/// Abstract base record for queries that retrieve all entities of a specific type.
/// Represents a query that returns a collection of entities of type TResponse.
/// This serves as a foundation for all get-all operations across the application modules.
/// </summary>
/// <typeparam name="TResponse">The type of entities being retrieved.</typeparam>
/// <remarks>
/// Get-all queries follow the Query pattern and are processed by MediatR.
/// They return a Result&lt;IEnumerable&lt;TResponse&gt;&gt; containing either the requested entities or error information.
/// Concrete implementations can add filtering, sorting, or pagination parameters.
/// </remarks>
public abstract record GetAllQuery<TResponse> : IRequest<Result<IEnumerable<TResponse>>>;
