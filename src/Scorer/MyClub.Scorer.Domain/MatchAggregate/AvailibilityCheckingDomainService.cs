// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;

namespace MyClub.Scorer.Domain.MatchAggregate
{
    public class AvailibilityCheckingDomainService(IProjectRepository projectRepository,
                                                   IMatchRepository matchRepository) : IAvailibilityCheckingDomainService
    {
        private readonly IMatchRepository _matchRepository = matchRepository;
        private readonly IProjectRepository _projectRepository = projectRepository;

        public IEnumerable<(ConflictType, Match, Match?)> GetAllConflicts()
        {
            var result = new List<(ConflictType, Match, Match?)>();
            var matches = _matchRepository.GetAll().Where(x => x.State != MatchState.Cancelled).ToList();
            var associations = matches.RoundRobin().SelectMany(x => x).ToList();
            foreach (var associationsGroupped in associations.GroupBy(x => x.item1))
            {
                foreach (var (match1, match2) in associationsGroupped)
                {
                    var conflicts = GetConflictsBetween(match1, match2);

                    result.AddRange(conflicts.Select(x => (x, match1, (Match?)match2)));
                }

                if (associationsGroupped.Key.Date.IsBefore(_projectRepository.GetCurrentOrThrow().StartDate))
                    result.Add((ConflictType.StartDatePassed, associationsGroupped.Key, null));

                if (associationsGroupped.Key.GetPeriod().End.IsAfter(_projectRepository.GetCurrentOrThrow().EndDate))
                    result.Add((ConflictType.EndDatePassed, associationsGroupped.Key, null));
            }

            return result;
        }

        public IEnumerable<ConflictType> GetConflictsBetween(Match match1, Match match2)
        {
            if (match1.State == MatchState.Cancelled || match2.State == MatchState.Cancelled) return [];

            var period1 = match1.GetPeriod();
            var period2 = match2.GetPeriod();

            var result = new List<ConflictType>();

            if (match2.Participate(match1.HomeTeam) || match2.Participate(match1.AwayTeam))
            {
                if (period1.Intersect(period2))
                    result.Add(ConflictType.TeamBusy);
                else
                {
                    var periodWithRestTime1 = new Period(period1.Start, period1.End + _projectRepository.GetCurrentOrThrow().Parameters.MinimumRestTime);
                    var periodWithRestTime2 = new Period(period2.Start, period2.End + _projectRepository.GetCurrentOrThrow().Parameters.MinimumRestTime);

                    if (periodWithRestTime1.Intersect(periodWithRestTime2))
                        result.Add(ConflictType.RestTimeNotRespected);
                }
            }

            if (match1.Stadium is not null && match1.Stadium.Equals(match2.Stadium))
            {
                if (period1.Intersect(period2))
                    result.Add(ConflictType.StadiumOccupancy);
                else
                {
                    var periodWithRotationTime1 = new Period(period1.Start, period1.End + _projectRepository.GetCurrentOrThrow().Parameters.RotationTime);
                    var periodWithRotationTime2 = new Period(period2.Start, period2.End + _projectRepository.GetCurrentOrThrow().Parameters.RotationTime);

                    if (periodWithRotationTime1.Intersect(periodWithRotationTime2))
                        result.Add(ConflictType.RotationTimeNotRespected);
                }
            }

            return result;
        }
    }
}
