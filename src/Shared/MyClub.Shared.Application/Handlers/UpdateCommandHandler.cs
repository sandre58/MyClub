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
using MyClub.Shared.Application.Persistence;
using MyClub.Shared.Kernel.Primitives;
using MyClub.Shared.Kernel.Repositories;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Shared.Application.Handlers;

public abstract class UpdateCommandHandler<TRepository, TEntity, TId, TUpdateCommand>(TRepository repository, IUnitOfWork unitOfWork, IMapper mapper) : IRequestHandler<TUpdateCommand, Result>
    where TRepository : IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : EntityId<TId>
    where TUpdateCommand : UpdateCommand
{
    public async Task<Result> Handle(TUpdateCommand command, CancellationToken cancellationToken)
    {
        var result = Update(command);

        if (result.IsFailure)
            return result;

        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    protected virtual Result Update(TUpdateCommand command)
    {
        var entity = repository.GetById(EntityId.From<TId>(command.Id));

        if (entity is null)
            return Failures.NotFound(command.Id.ToString());

        Map(command, entity);

        repository.Update(entity);

        return Result.Success();
    }

    protected virtual void Map(TUpdateCommand cmd, TEntity entity) => mapper.Map(cmd, entity);
}
