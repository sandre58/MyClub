// -----------------------------------------------------------------------
// <copyright file="GetByIdQueryHandler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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
/// Abstract base class for handling get-by-id queries in the CQRS pattern.
/// Provides a template method implementation for retrieving entities by ID with automatic mapping.
/// </summary>
/// <typeparam name="TRepository">The repository type for the entity being retrieved.</typeparam>
/// <typeparam name="TEntity">The entity type being retrieved.</typeparam>
/// <typeparam name="TId">The strongly-typed identifier for the entity.</typeparam>
/// <typeparam name="TResponse">The response DTO type.</typeparam>
/// <typeparam name="TQuery">The specific get-by-id query type being handled.</typeparam>
/// <param name="repository">The repository for retrieving the entity.</param>
/// <param name="mapper">The AutoMapper instance for entity-to-DTO mapping.</param>
/// <remarks>
/// This handler follows the Template Method pattern, allowing derived classes to customize
/// the retrieval process while maintaining consistent entity access and error handling.
/// The handler automatically retrieves the entity by ID and maps it to the response DTO.
/// </remarks>
public abstract class GetByIdQueryHandler<TRepository, TEntity, TId, TResponse, TQuery>(TRepository repository, IMapper mapper) : IRequestHandler<TQuery, Result<TResponse>>
    where TRepository : IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : EntityId<TId>
    where TQuery : GetByIdQuery<TResponse>
{
    /// <summary>
    /// Handles the get-by-id query by retrieving the entity and mapping it to the response DTO.
    /// </summary>
    /// <param name="query">The get-by-id query containing the entity ID.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result containing the mapped response DTO, or error information if the entity was not found.</returns>
    public virtual async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
    {
        var result = await GetByIdAsync(query, cancellationToken).ConfigureAwait(false);

        return result.IsFailure ? Result.Fail<TResponse>(result) : Result.Success(result.Value);
    }

    /// <summary>
    /// Retrieves the entity by ID and maps it to the response DTO.
    /// This method can be overridden by derived classes to customize the retrieval logic.
    /// </summary>
    /// <param name="query">The get-by-id query containing the entity ID.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result containing the mapped response DTO, or error information if the entity was not found.</returns>
    protected virtual async Task<Result<TResponse>> GetByIdAsync(TQuery query, CancellationToken cancellationToken)
    {
        var entity = await Task.Run(() => repository.GetById(EntityId.From<TId>(query.Id)), cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return Failures.NotFound<TResponse>(query.Id.ToString());

        var response = Map(entity);
        return Result.Success(response);
    }

    /// <summary>
    /// Maps the entity to the response DTO using AutoMapper.
    /// This method can be overridden by derived classes to customize the mapping logic.
    /// </summary>
    /// <param name="entity">The entity to map.</param>
    /// <returns>The mapped response DTO.</returns>
    protected virtual TResponse Map(TEntity entity) => mapper.Map<TResponse>(entity);
}
