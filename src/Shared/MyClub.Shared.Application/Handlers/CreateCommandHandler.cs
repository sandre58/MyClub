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
using MyClub.Shared.Application.Persistence;
using MyClub.Shared.Kernel.Primitives;
using MyClub.Shared.Kernel.Repositories;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Shared.Application.Handlers;

public abstract class CreateCommandHandler<TRepository, TEntity, TId, TCreateCommand>(TRepository repository, IUnitOfWork unitOfWork, IMapper mapper) : IRequestHandler<TCreateCommand, Result<Guid>>
    where TRepository : IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : EntityId<TId>
    where TCreateCommand : CreateCommand
{
    public async Task<Result<Guid>> Handle(TCreateCommand command, CancellationToken cancellationToken)
    {
        var result = Add(command);

        if (result.IsFailure)
            return Result.Fail<Guid>(result);

        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(result.Value.Value);
    }

    protected virtual Result<TId> Add(TCreateCommand command)
    {
        var entity = Map(command);
        repository.Add(entity);

        return Result.Success(entity.Id);
    }

    protected virtual TEntity Map(TCreateCommand cmd) => mapper.Map<TEntity>(cmd);
}
