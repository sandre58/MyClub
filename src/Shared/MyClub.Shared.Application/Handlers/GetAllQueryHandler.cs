// -----------------------------------------------------------------------
// <copyright file="GetAllQueryHandler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using MyClub.Shared.Application.Queries;
using MyClub.Shared.Kernel.Primitives;
using MyClub.Shared.Kernel.Repositories;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Shared.Application.Handlers;

/// <summary>
/// Abstract base class for handling get-all queries in the CQRS pattern.
/// Provides a template method implementation for retrieving all entities with automatic mapping.
/// </summary>
/// <typeparam name="TRepository">The repository type for the entities being retrieved.</typeparam>
/// <typeparam name="TEntity">The entity type being retrieved.</typeparam>
/// <typeparam name="TId">The strongly-typed identifier for the entity.</typeparam>
/// <typeparam name="TResponse">The response DTO type.</typeparam>
/// <typeparam name="TQuery">The specific get-all query type being handled.</typeparam>
/// <param name="repository">The repository for retrieving the entities.</param>
/// <param name="mapper">The AutoMapper instance for entity-to-DTO mapping.</param>
/// <remarks>
/// This handler follows the Template Method pattern, allowing derived classes to customize
/// the retrieval process while maintaining consistent entity access and error handling.
/// The handler automatically retrieves all entities and maps them to response DTOs.
/// </remarks>
public abstract class GetAllQueryHandler<TRepository, TEntity, TId, TResponse, TQuery>(TRepository repository, IMapper mapper) : IRequestHandler<TQuery, Result<IEnumerable<TResponse>>>
    where TRepository : IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : EntityId<TId>
    where TQuery : GetAllQuery<TResponse>
{
    /// <summary>
    /// Handles the get-all query by retrieving all entities and mapping them to response DTOs.
    /// </summary>
    /// <param name="query">The get-all query.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result containing the collection of mapped response DTOs, or error information if the operation failed.</returns>
    public virtual async Task<Result<IEnumerable<TResponse>>> Handle(TQuery query, CancellationToken cancellationToken)
    {
        var result = await GetAllAsync(query, cancellationToken).ConfigureAwait(false);

        return result.IsFailure ? Result.Fail<IEnumerable<TResponse>>(result) : Result.Success(result.Value);
    }

    /// <summary>
    /// Retrieves all entities and maps them to response DTOs.
    /// This method can be overridden by derived classes to customize the retrieval logic,
    /// such as adding filtering, sorting, or additional business rules.
    /// </summary>
    /// <param name="query">The get-all query.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result containing the collection of mapped response DTOs, or error information if the operation failed.</returns>
    protected virtual async Task<Result<IEnumerable<TResponse>>> GetAllAsync(TQuery query, CancellationToken cancellationToken)
    {
        var entities = await Task.Run(() => repository.GetAll(), cancellationToken).ConfigureAwait(false);
        var responses = Map(entities);

        return Result.Success(responses);
    }

    /// <summary>
    /// Maps the collection of entities to response DTOs using AutoMapper.
    /// This method can be overridden by derived classes to customize the mapping logic.
    /// </summary>
    /// <param name="entities">The entities to map.</param>
    /// <returns>The collection of mapped response DTOs.</returns>
    protected virtual IEnumerable<TResponse> Map(IEnumerable<TEntity> entities) => entities.Select(mapper.Map<TResponse>);
}
