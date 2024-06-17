// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;

namespace MyClub.Scorer.Application.Services
{
    public class AvailibilityCheckingService(IProjectRepository projectRepository,
                                             IMatchRepository matchRepository,
                                             IAvailibilityCheckingDomainService availibilityCheckingDomainService)
    {
        private readonly IAvailibilityCheckingDomainService _availibilityCheckingDomainService = availibilityCheckingDomainService;
        private readonly IMatchRepository _matchRepository = matchRepository;
        private readonly IProjectRepository _projectRepository = projectRepository;

        public List<(ConflictType, Guid, Guid?)> GetAllConflicts()
            => _availibilityCheckingDomainService.GetAllConflicts().Select(x => (x.Item1, x.Item2.Id, x.Item3?.Id)).ToList();

        public AvailabilityCheck GetTeamsAvaibility(DateTime date,
                                                    IEnumerable<Guid> teamIds,
                                                    MatchFormat? matchFormat = null,
                                                    IEnumerable<Guid>? excludeMatchIds = null)
        {
            var period = GetPeriodOfFullMatch(date, matchFormat);

            if (!TeamsAreAvailable(teamIds, period, excludeMatchIds))
                return AvailabilityCheck.IsBusy;

            period = GetPeriodOfFullMatch(date, matchFormat, _projectRepository.GetCurrentOrThrow().Parameters.MinimumRestTime);

            return !TeamsAreAvailable(teamIds, period, excludeMatchIds) ? AvailabilityCheck.IsPartiallyBusy : AvailabilityCheck.IsAvailable;
        }

        public AvailabilityCheck GetStadiumAvaibility(Guid stadiumId,
                                                      DateTime dateOfMatch,
                                                      MatchFormat? matchFormat = null,
                                                      IEnumerable<Guid>? excludeMatchIds = null)
        {
            var period = GetPeriodOfFullMatch(dateOfMatch, matchFormat);

            if (!StadiumIsAvailable(stadiumId, period, excludeMatchIds))
                return AvailabilityCheck.IsBusy;

            period = GetPeriodOfFullMatch(dateOfMatch, matchFormat, _projectRepository.GetCurrentOrThrow().Parameters.RotationTime);

            return !StadiumIsAvailable(stadiumId, period, excludeMatchIds) ? AvailabilityCheck.IsPartiallyBusy : AvailabilityCheck.IsAvailable;
        }

        public bool StadiumIsAvailable(Guid stadiumId, Period period, IEnumerable<Guid>? excludeMatchIds = null)
            => GetMatchesInStadium(stadiumId, period, excludeMatchIds).Count == 0;

        public bool TeamsAreAvailable(IEnumerable<Guid> teamIds, Period period, IEnumerable<Guid>? excludeMatchIds = null)
            => GetMatchesOfTeams(teamIds, period, excludeMatchIds).Count == 0;

        private Period GetPeriodOfFullMatch(DateTime date, MatchFormat? matchFormat = null, TimeSpan? timeToAdd = null)
        {
            var matchFullTime = (matchFormat ?? _projectRepository.GetCurrentOrThrow().Competition.ProvideFormat()).GetFullTime();

            return timeToAdd.HasValue
                ? new Period(date.ToUniversalTime().SubtractFluentTimeSpan(timeToAdd.Value), date.ToUniversalTime().AddFluentTimeSpan(matchFullTime + timeToAdd.Value))
                : date.ToUniversalTime().ToPeriod(matchFullTime);
        }

        private List<Match> GetMatchesInStadium(Guid stadiumId,
                                               Period period,
                                               IEnumerable<Guid>? excludedMatchIds = null)
        {
            var matches = _matchRepository.GetMatchesInStadium(stadiumId, period).Where(x => x.State != MatchState.Cancelled);

            if (excludedMatchIds is not null)
            {
                var excludedMatches = matches.Where(x => excludedMatchIds.Contains(x.Id)).ToList();
                matches = matches.Except(excludedMatches);
            }

            return matches.ToList();
        }

        private List<Match> GetMatchesOfTeams(IEnumerable<Guid> teamIds,
                                               Period period,
                                               IEnumerable<Guid>? excludedMatchIds = null)
        {
            var matches = _matchRepository.GetMatchesOfTeams(teamIds, period).Where(x => x.State != MatchState.Cancelled);

            if (excludedMatchIds is not null)
            {
                var excludedMatches = matches.Where(x => excludedMatchIds.Contains(x.Id)).ToList();
                matches = matches.Except(excludedMatches);
            }

            return matches.ToList();
        }
    }
}
