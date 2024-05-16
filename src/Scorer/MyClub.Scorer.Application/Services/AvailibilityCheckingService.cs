// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using MyNet.Utilities.Messaging;
using MyClub.Domain.Enums;
using MyClub.Scorer.Application.Messages;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;

namespace MyClub.Scorer.Application.Services
{
    public class AvailibilityCheckingService(IProjectRepository projectRepository,
                                             IMatchRepository matchRepository,
                                             IMatchDomainService matchDomainService)
    {
        private readonly IMatchDomainService _matchDomainService = matchDomainService;
        private readonly IMatchRepository _matchRepository = matchRepository;
        private readonly IProjectRepository _projectRepository = projectRepository;

        public void CheckAllConflicts()
        {
            var conflicts = _matchDomainService.GetAllConflicts().Select(x => (x.Item1, x.Item2.Id, x.Item3?.Id));

            Messenger.Default.Send(new MatchConflictsValidationMessage(conflicts));
        }

        public AvailabilityCheck GetTeamsAvaibility(IEnumerable<Guid> teamIds,
                                                   DateTime dateOfMatch,
                                                   MatchFormat? matchFormat = null,
                                                   IEnumerable<Guid>? excludeMatchIds = null)
        {
            var period = GetPeriodOfFullMatch(dateOfMatch, matchFormat);

            if (!TeamsAreAvailable(teamIds, period, excludeMatchIds))
                return AvailabilityCheck.IsBusy;

            period = GetPeriodOfFullMatch(dateOfMatch, matchFormat, _projectRepository.GetCurrentOrThrow().Parameters.MinimumRestTime);

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
            => GetMatchesInPeriod(period, stadiumId: stadiumId, excludedMatchIds: excludeMatchIds).Count == 0;

        public bool TeamsAreAvailable(IEnumerable<Guid> teamIds, Period period, IEnumerable<Guid>? excludeMatchIds = null)
            => GetMatchesInPeriod(period, teamIds: teamIds, excludedMatchIds: excludeMatchIds).Count == 0;

        private Period GetPeriodOfFullMatch(DateTime date, MatchFormat? matchFormat = null, TimeSpan? timeToAdd = null)
        {
            var matchFullTime = (matchFormat ?? _projectRepository.GetCurrentOrThrow().Competition.ProvideFormat()).GetFullTime();

            return timeToAdd.HasValue
                ? new Period(date.ToUniversalTime().SubtractFluentTimeSpan(timeToAdd.Value), date.ToUniversalTime().AddFluentTimeSpan(matchFullTime + timeToAdd.Value))
                : date.ToUniversalTime().ToPeriod(matchFullTime);
        }

        private List<Match> GetMatchesInPeriod(Period period,
                                               Guid? stadiumId = null,
                                               IEnumerable<Guid>? teamIds = null,
                                               IEnumerable<Guid>? excludedMatchIds = null)
        {
            var matches = _matchRepository.GetByPeriod(period);

            if (stadiumId is not null)
                matches = matches.Where(x => x.Stadium is not null && x.Stadium.Id == stadiumId);

            if (teamIds is not null)
                matches = matches.Where(x => teamIds.Any(y => x.Participate(y)));

            if (excludedMatchIds is not null)
            {
                var excludedMatches = matches.Where(x => excludedMatchIds.Contains(x.Id)).ToList();
                matches = matches.Except(excludedMatches);
            }

            return matches.ToList();
        }
    }
}
