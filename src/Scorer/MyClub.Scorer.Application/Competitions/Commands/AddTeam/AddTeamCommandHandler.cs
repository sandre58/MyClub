// -----------------------------------------------------------------------
// <copyright file="AddTeamCommandHandler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.CompetitionAggregate.Repositories;
using MyClub.Scorer.Domain.CompetitionAggregate.Teams;
using MyClub.Shared.Application.Persistence;
using MyClub.Shared.Domain.Stadiums;
using MyClub.Shared.Kernel.Primitives;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Scorer.Application.Competitions.Commands.AddTeam;

public class AddTeamCommandHandler(ICompetitionRepository competitionRepository, IUnitOfWork unitOfWork, IMapper mapper) : IRequestHandler<AddTeamCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddTeamCommand command, CancellationToken cancellationToken)
    {
        var competitionId = EntityId.From<CompetitionId>(command.CompetitionId);
        var competition = competitionRepository.GetById(competitionId);

        if (competition is null)
            return Failures.NotFound<Guid>(command.CompetitionId.ToString());

        // Check Stadium
        if (command.StadiumId.HasValue)
        {
            if (competition.HasStadium(EntityId.From<StadiumId>(command.StadiumId.Value)))
                return Failures.NotFound<Guid>(command.StadiumId.Value.ToString());
        }

        // Check Name
        if (competition.HasSimilarTeams(command.Name))
            return Failures.NameAlreadyExists<Guid>(command.Name);

        var team = mapper.Map<Team>(command);
        var result = competition.AddTeam(team);

        if (result.IsFailure)
            return Result.Fail<Guid>(result);

        competitionRepository.Update(competition);

        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(team.Id.Value);
    }
}
