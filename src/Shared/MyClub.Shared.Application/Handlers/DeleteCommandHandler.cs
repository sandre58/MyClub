// -----------------------------------------------------------------------
// <copyright file="DeleteCommandHandler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MyClub.Shared.Application.Commands;
using MyClub.Shared.Application.Persistence;
using MyClub.Shared.Kernel.Primitives;
using MyClub.Shared.Kernel.Repositories;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Shared.Application.Handlers;

public abstract class DeleteCommandHandler<TRepository, TEntity, TId, TDeleteCommand>(TRepository repository, IUnitOfWork unitOfWork) : IRequestHandler<TDeleteCommand, Result>
    where TRepository : IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : EntityId<TId>
    where TDeleteCommand : DeleteCommand
{
    public async Task<Result> Handle(TDeleteCommand command, CancellationToken cancellationToken)
    {
        var result = Delete(command);

        if (result.IsFailure)
            return result;

        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    protected virtual Result Delete(TDeleteCommand command)
    {
        repository.Delete(EntityId.From<TId>(command.Id));

        return Result.Success();
    }
}
