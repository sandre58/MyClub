// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Infrastructure.Repositories
{
    public class SchedulingParametersRepository(IProjectRepository projectRepository) : ISchedulingParametersRepository
    {
        private readonly IProjectRepository _projectRepository = projectRepository;

        public SchedulingParameters GetByMatch(Match match)
        {
            if (_projectRepository.GetCurrentOrThrow() is LeagueProject leagueProject)
                return leagueProject.SchedulingParameters;

            if (_projectRepository.GetCurrentOrThrow() is CupProject cupProject)
            {
                var round = cupProject.Competition.Rounds.First(x => x.GetAllMatches().Contains(match));
                return cupProject.GetSchedulingParameters(round.Id);
            }

            if (_projectRepository.GetCurrentOrThrow() is TournamentProject tournamentProject)
            {
                var stage = tournamentProject.Competition.Stages.First(x => x.GetAllMatches().Contains(match));
                return tournamentProject.GetSchedulingParameters(stage.Id);
            }

            throw new InvalidOperationException("Not implemented matches provider");
        }

        public SchedulingParameters GetByMatchdaysProvider(IMatchdaysProvider matchdaysProvider)
            => matchdaysProvider is League
                ? _projectRepository.GetCurrentOrThrow().CastIn<LeagueProject>().SchedulingParameters
                : matchdaysProvider is IStage stage
                ? _projectRepository.GetCurrentOrThrow().CastIn<TournamentProject>().GetSchedulingParameters(stage.Id)
                : throw new InvalidOperationException("Not implemented matchdays provider");
    }
}
