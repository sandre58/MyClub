// -----------------------------------------------------------------------
// <copyright file="CreateCommandHandler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
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
/// Abstract base class for handling create commands in the CQRS pattern.
/// Provides a template method implementation for creating entities with automatic mapping,
/// repository persistence, and unit of work management.
/// </summary>
/// <typeparam name="TRepository">The repository type for the entity being created.</typeparam>
/// <typeparam name="TEntity">The entity type being created.</typeparam>
/// <typeparam name="TId">The strongly-typed identifier for the entity.</typeparam>
/// <typeparam name="TCreateCommand">The specific create command type being handled.</typeparam>
/// <param name="repository">The repository for persisting the entity.</param>
/// <param name="unitOfWork">The unit of work for transaction management.</param>
/// <param name="mapper">The AutoMapper instance for command-to-entity mapping.</param>
/// <remarks>
/// This handler follows the Template Method pattern, allowing derived classes to customize
/// the creation process while maintaining consistent transaction and error handling.
/// The handler automatically maps the command to an entity, adds it to the repository,
/// and commits the transaction.
/// </remarks>
public abstract class CreateCommandHandler<TRepository, TEntity, TId, TCreateCommand>(TRepository repository, IUnitOfWork unitOfWork, IMapper mapper) : IRequestHandler<TCreateCommand, Result<Guid>>
    where TRepository : IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : EntityId<TId>
    where TCreateCommand : CreateCommand
{
    /// <summary>
    /// Handles the create command by adding the entity to the repository and committing the transaction.
    /// </summary>
    /// <param name="command">The create command containing the data for the new entity.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A Result containing the GUID of the created entity, or error information if the operation failed.</returns>
    public async Task<Result<Guid>> Handle(TCreateCommand command, CancellationToken cancellationToken)
    {
        var result = Add(command);

        if (result.IsFailure)
            return Result.Fail<Guid>(result);

        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(result.Value.Value);
    }

    /// <summary>
    /// Adds the entity to the repository based on the create command.
    /// This method can be overridden by derived classes to customize the creation logic.
    /// </summary>
    /// <param name="command">The create command containing the entity data.</param>
    /// <returns>A Result containing the strongly-typed entity ID, or error information if the operation failed.</returns>
    protected virtual Result<TId> Add(TCreateCommand command)
    {
        var entity = Map(command);
        repository.Add(entity);

        return Result.Success(entity.Id);
    }

    /// <summary>
    /// Maps the create command to an entity using AutoMapper.
    /// This method can be overridden by derived classes to customize the mapping logic.
    /// </summary>
    /// <param name="cmd">The create command to map.</param>
    /// <returns>The mapped entity ready for persistence.</returns>
    protected virtual TEntity Map(TCreateCommand cmd) => mapper.Map<TEntity>(cmd);
}
