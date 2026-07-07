// -----------------------------------------------------------------------
// <copyright file="DeleteTeamCommandHandler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.CompetitionAggregate.Repositories;
using MyClub.Shared.Kernel.Persistence;
using MyClub.Shared.Kernel.Primitives;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Scorer.Application.Competitions.Commands.DeleteTeam;

public class DeleteTeamCommandHandler(ICompetitionRepository competitionRepository, IUnitOfWork unitOfWork) : IRequestHandler<DeleteTeamCommand, Result>
{
    public async Task<Result> Handle(DeleteTeamCommand command, CancellationToken cancellationToken)
    {
        var competitionId = EntityId.From<CompetitionId>(command.CompetitionId);
        var competition = await competitionRepository.GetByIdAsync(competitionId, cancellationToken).ConfigureAwait(false);

        if (competition is null)
            return Failures.NotFound<Guid>(command.CompetitionId.ToString());

        await competitionRepository.GetByIdAsync(competitionId, cancellationToken).ConfigureAwait(false);

        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
