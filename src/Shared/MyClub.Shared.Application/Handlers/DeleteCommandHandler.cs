// -----------------------------------------------------------------------
// <copyright file="DeleteCommandHandler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MyClub.Shared.Application.Commands;
using MyClub.Shared.Kernel.Persistence;
using MyClub.Shared.Kernel.Primitives;
using MyClub.Shared.Kernel.Repositories;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Shared.Application.Handlers;

/// <summary>
/// Abstract base class for handling delete commands in the CQRS pattern.
/// Provides a template method implementation for deleting entities with automatic
/// repository deletion and unit of work management.
/// </summary>
/// <typeparam name="TRepository">The repository type for the entity being deleted.</typeparam>
/// <typeparam name="TEntity">The entity type being deleted.</typeparam>
/// <typeparam name="TId">The strongly-typed identifier for the entity.</typeparam>
/// <typeparam name="TDeleteCommand">The specific delete command type being handled.</typeparam>
/// <param name="repository">The repository for managing the entity persistence.</param>
/// <param name="unitOfWork">The unit of work for transaction management.</param>
/// <remarks>
/// This handler follows the Template Method pattern, allowing derived classes to customize
/// the deletion process while maintaining consistent transaction and error handling.
/// The handler automatically deletes the entity by ID and commits the transaction.
/// The actual deletion strategy (hard delete, soft delete, etc.) depends on the repository implementation.
/// </remarks>
public abstract class DeleteCommandHandler<TRepository, TEntity, TId, TDeleteCommand>(TRepository repository, IUnitOfWork unitOfWork) : IRequestHandler<TDeleteCommand, Result>
    where TRepository : IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : EntityId<TId>
    where TDeleteCommand : DeleteCommand
{
    /// <summary>
    /// Handles the delete command by removing the entity and committing the transaction.
    /// </summary>
    /// <param name="command">The delete command containing the ID of the entity to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result indicating success or failure of the delete operation.</returns>
    public async Task<Result> Handle(TDeleteCommand command, CancellationToken cancellationToken)
    {
        var result = await DeleteAsync(command, cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
            return result;

        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    /// <summary>
    /// Deletes the entity based on the delete command.
    /// This method can be overridden by derived classes to customize the deletion logic,
    /// such as implementing soft deletes or checking business rules before deletion.
    /// </summary>
    /// <param name="command">The delete command containing the ID of the entity to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result indicating success or failure of the delete operation.</returns>
    protected virtual async Task<Result> DeleteAsync(TDeleteCommand command, CancellationToken cancellationToken = default)
    {
        await repository.DeleteAsync(EntityId.From<TId>(command.Id), cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
