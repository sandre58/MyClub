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
using MyClub.Shared.Domain.Stadiums;
using MyClub.Shared.Kernel.Persistence;
using MyClub.Shared.Kernel.Primitives;
using MyClub.Shared.Kernel.Results;

namespace MyClub.Scorer.Application.Competitions.Commands.AddTeam;

/// <summary>
/// Handles the execution of AddTeamCommand by orchestrating domain operations,
/// validation, and persistence to add a new team to a competition.
/// This handler implements the CQRS Command Handler pattern with comprehensive error handling.
/// </summary>
/// <param name="competitionRepository">Repository for accessing and persisting competition aggregates.</param>
/// <param name="unitOfWork">Unit of work for managing transactional consistency.</param>
/// <param name="mapper">AutoMapper instance for mapping commands to domain entities.</param>
public class AddTeamCommandHandler(ICompetitionRepository competitionRepository, IUnitOfWork unitOfWork, IMapper mapper) : IRequestHandler<AddTeamCommand, Result<Guid>>
{
    /// <summary>
    /// Handles the AddTeamCommand by coordinating domain operations to add a team to a competition.
    /// </summary>
    /// <param name="command">The command containing team information and target competition ID.</param>
    /// <param name="cancellationToken">Cancellation token to support operation cancellation.</param>
    /// <returns>
    /// A Result containing the new team ID if successful, or failure information if the operation fails.
    /// </returns>
    public async Task<Result<Guid>> Handle(AddTeamCommand command, CancellationToken cancellationToken)
    {
        // Step 1: Load the target competition aggregate
        var competitionId = EntityId.From<CompetitionId>(command.CompetitionId);
        var competition = competitionRepository.GetById(competitionId);

        // Step 2: Validate competition existence
        if (competition is null)
            return Failures.NotFound<Guid>(command.CompetitionId.ToString());

        // Step 3: Validate stadium reference if provided
        if (command.StadiumId.HasValue)
        {
            if (competition.HasStadium(EntityId.From<StadiumId>(command.StadiumId.Value)))
                return Failures.NotFound<Guid>(command.StadiumId.Value.ToString());
        }

        // Step 4: Check for duplicate team names
        if (competition.HasSimilarTeams(command.Name))
            return Failures.NameAlreadyExists<Guid>(command.Name);

        // Step 5: Map command to domain entity
        var team = mapper.Map<Team>(command);

        // Step 6: Execute domain operation
        var result = competition.AddTeam(team);

        if (result.IsFailure)
            return Result.Fail<Guid>(result);

        // Step 7: Persist changes
        competitionRepository.Update(competition);
        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);

        // Step 8: Return success with new team ID
        return Result.Success(team.Id.Value);
    }
}
