// -----------------------------------------------------------------------
// <copyright file="UpdateCommandHandler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using MyClub.Shared.Application.Commands;
using MyClub.Shared.Kernel.Persistence;
using MyClub.Shared.Kernel.Primitives;
using MyClub.Shared.Kernel.Repositories;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Shared.Application.Handlers;

/// <summary>
/// Abstract base class for handling update commands in the CQRS pattern.
/// Provides a template method implementation for updating entities with automatic entity retrieval,
/// mapping, repository persistence, and unit of work management.
/// </summary>
/// <typeparam name="TRepository">The repository type for the entity being updated.</typeparam>
/// <typeparam name="TEntity">The entity type being updated.</typeparam>
/// <typeparam name="TId">The strongly-typed identifier for the entity.</typeparam>
/// <typeparam name="TUpdateCommand">The specific update command type being handled.</typeparam>
/// <param name="repository">The repository for persisting the entity.</param>
/// <param name="unitOfWork">The unit of work for transaction management.</param>
/// <param name="mapper">The AutoMapper instance for command-to-entity mapping.</param>
/// <remarks>
/// This handler follows the Template Method pattern, allowing derived classes to customize
/// the update process while maintaining consistent entity retrieval, transaction, and error handling.
/// The handler automatically retrieves the entity, maps the command data to it,
/// updates it in the repository, and commits the transaction.
/// </remarks>
public abstract class UpdateCommandHandler<TRepository, TEntity, TId, TUpdateCommand>(TRepository repository, IUnitOfWork unitOfWork, IMapper mapper) : IRequestHandler<TUpdateCommand, Result>
    where TRepository : IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : EntityId<TId>
    where TUpdateCommand : UpdateCommand
{
    /// <summary>
    /// Handles the update command by retrieving the entity, applying changes, and committing the transaction.
    /// </summary>
    /// <param name="command">The update command containing the entity ID and updated data.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result indicating success or failure of the update operation.</returns>
    public async Task<Result> Handle(TUpdateCommand command, CancellationToken cancellationToken)
    {
        var result = await UpdateAsync(command, cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
            return result;

        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    /// <summary>
    /// Updates the entity based on the update command.
    /// Retrieves the entity by ID, maps the command data to it, and marks it for update in the repository.
    /// This method can be overridden by derived classes to customize the update logic.
    /// </summary>
    /// <param name="command">The update command containing the entity ID and data to update.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result indicating success or failure of the update operation.</returns>
    protected virtual async Task<Result> UpdateAsync(TUpdateCommand command, CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetByIdAsync(EntityId.From<TId>(command.Id), cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return Failures.NotFound(command.Id.ToString());

        Map(command, entity);

        await repository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    /// <summary>
    /// Maps the update command data to the existing entity using AutoMapper.
    /// This method can be overridden by derived classes to customize the mapping logic.
    /// </summary>
    /// <param name="cmd">The update command containing the new data.</param>
    /// <param name="entity">The existing entity to update.</param>
    protected virtual void Map(TUpdateCommand cmd, TEntity entity) => mapper.Map(cmd, entity);
}
