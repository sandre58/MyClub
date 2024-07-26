// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class SchedulingDomainService(IMatchRepository matchRepository, ITeamRepository teamRepository, IStadiumRepository stadiumRepository) : ISchedulingDomainService
    {
        private readonly IMatchRepository _matchRepository = matchRepository;
        private readonly ITeamRepository _teamRepository = teamRepository;
        private readonly IStadiumRepository _stadiumRepository = stadiumRepository;

        public IEnumerable<(ConflictType, Match, Match?)> GetAllConflicts()
        {
            var result = new List<(ConflictType, Match, Match?)>();
            var matches = _matchRepository.GetAll().Where(x => x.State != MatchState.Cancelled).ToList();
            var associations = matches.RoundRobin().SelectMany(x => x).ToList();
            foreach (var associationsGrouped in associations.GroupBy(x => x.item1))
            {
                foreach (var (match1, match2) in associationsGrouped)
                {
                    var conflicts = GetConflictsBetween(match1, match2);

                    result.AddRange(conflicts.Select(x => (x, match1, (Match?)match2)));
                }

                var schedulingParameters = associationsGrouped.Key.GetSchedulingParameters();
                if (associationsGrouped.Key.Date.IsBefore(schedulingParameters.StartDate))
                    result.Add((ConflictType.StartDatePassed, associationsGrouped.Key, null));

                if (associationsGrouped.Key.GetPeriod().End.IsAfter(schedulingParameters.EndDate))
                    result.Add((ConflictType.EndDatePassed, associationsGrouped.Key, null));
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
                if (period1.IntersectWith(period2))
                    result.Add(ConflictType.TeamBusy);
                else
                {
                    var periodWithRestTime1 = new Period(period1.Start, period1.End + match1.GetRestTime());
                    var periodWithRestTime2 = new Period(period2.Start, period2.End + match2.GetRestTime());

                    if (periodWithRestTime1.IntersectWith(periodWithRestTime2))
                        result.Add(ConflictType.RestTimeNotRespected);
                }
            }

            if (match1.Stadium is not null && match1.Stadium.Equals(match2.Stadium))
            {
                if (period1.IntersectWith(period2))
                    result.Add(ConflictType.StadiumOccupancy);
                else
                {
                    var periodWithRotationTime1 = new Period(period1.Start, period1.End + match1.GetRotationTime());
                    var periodWithRotationTime2 = new Period(period2.Start, period2.End + match2.GetRotationTime());

                    if (periodWithRotationTime1.IntersectWith(periodWithRotationTime2))
                        result.Add(ConflictType.RotationTimeNotRespected);
                }
            }

            return result;
        }

        public bool TeamsAreAvailable(IEnumerable<Guid> teamIds, Period period, bool withRestTime = false, IEnumerable<Guid>? ignoredMatchIds = null)
        {
            var teams = _teamRepository.GetAll().Where(x => teamIds.Contains(x.Id)).ToList();
            var matches = _matchRepository.GetMatchesOfTeams(teamIds);

            if (ignoredMatchIds is not null)
            {
                var excludedMatches = matches.Where(x => ignoredMatchIds.Contains(x.Id)).ToList();
                matches = matches.Except(excludedMatches);
            }

            return teams.All(x => x.IsAvailable(matches, period, withRestTime));
        }

        public bool StadiumIsAvailable(Guid stadiumId, Period period, bool withRotationTime = false, IEnumerable<Guid>? ignoredMatchIds = null)
        {
            var stadium = _stadiumRepository.GetById(stadiumId);
            var matches = _matchRepository.GetMatchesInStadium(stadiumId);

            if (ignoredMatchIds is not null)
            {
                var excludedMatches = matches.Where(x => ignoredMatchIds.Contains(x.Id)).ToList();
                matches = matches.Except(excludedMatches);
            }

            return stadium?.IsAvailable(matches, period, withRotationTime) ?? false;
        }
    }
}
