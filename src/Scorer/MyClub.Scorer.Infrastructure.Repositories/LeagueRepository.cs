// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Domain.Services;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.RankingAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Sequences;

namespace MyClub.Scorer.Infrastructure.Repositories
{
    public class LeagueRepository(IProjectRepository projectRepository, IAuditService auditService) : ILeagueRepository
    {
        private readonly IProjectRepository _projectRepository = projectRepository;
        private readonly IAuditService _auditService = auditService;

        public bool HasCurrent() => _projectRepository.HasCurrent() && _projectRepository.GetCompetition() is League;

        public League GetCurrentOrThrow() => _projectRepository.GetCompetition().CastIn<League>() ?? throw new InvalidOperationException($"Current competition is not league");

        public void UpdateRankingRules(RankingRules rules)
        {
            var league = GetCurrentOrThrow();
            league.RankingRules = rules;

            _auditService.Update(league);
        }

        public void UpdateLabels(IDictionary<AcceptableValueRange<int>, RankLabel> labels)
        {
            var league = GetCurrentOrThrow();

            league.Labels.Clear();
            league.Labels.AddRange(labels);

            _auditService.Update(league);
        }

        public void UpdatePenaltyPoints(Dictionary<Guid, int> penaltyPoints)
        {
            var league = GetCurrentOrThrow();

            league.ClearPenaltyPoints();

            penaltyPoints.ForEach(x => league.AddPenalty(x.Key, x.Value));

            _auditService.Update(league);
        }

        public Matchday InsertMatchday(DateTime date, string name, string? shortName = null)
        {
            var added = GetCurrentOrThrow().AddMatchday(date, name, shortName);

            _auditService.New(added);

            return added;
        }

        public void Fill(IEnumerable<Matchday> matchdays)
        {
            var league = GetCurrentOrThrow();
            league.Clear();

            foreach (var item in matchdays)
            {
                var matchday = league.AddMatchday(item);
                _auditService.New(matchday);
            }

            _auditService.Update(league);
        }
    }
}
