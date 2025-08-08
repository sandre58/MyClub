// -----------------------------------------------------------------------
// <copyright file="UpdateTeamCommandHandler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.CompetitionAggregate.Repositories;
using MyClub.Shared.Domain.Stadiums;
using MyClub.Shared.Kernel.Persistence;
using MyClub.Shared.Kernel.Primitives;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Scorer.Application.Competitions.Commands.UpdateTeam;

public class UpdateTeamCommandHandler(ICompetitionRepository competitionRepository, IUnitOfWork unitOfWork, IMapper mapper) : IRequestHandler<UpdateTeamCommand, Result>
{
    public async Task<Result> Handle(UpdateTeamCommand command, CancellationToken cancellationToken)
    {
        var competitionId = EntityId.From<CompetitionId>(command.CompetitionId);
        var competition = competitionRepository.GetById(competitionId);

        if (competition is null)
            return Failures.NotFound<Guid>(command.CompetitionId.ToString());

        var team = competition.Teams.FirstOrDefault(t => t.Id == command.Id);
        if (team is null)
            return Failures.NotFound<Guid>(command.Id.ToString());

        // Check Stadium
        if (command.StadiumId.HasValue)
        {
            if (competition.HasStadium(EntityId.From<StadiumId>(command.StadiumId.Value)))
                return Failures.NotFound<Guid>(command.StadiumId.Value.ToString());
        }

        // Check Name
        if (competition.HasSimilarTeams(command.Name))
            return Failures.NameAlreadyExists<Guid>(command.Name);

        _ = mapper.Map(command, team);

        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
