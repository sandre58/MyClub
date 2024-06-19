﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Enums;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;

namespace MyClub.Scorer.Application.Services
{
    public class AvailibilityCheckingService(IMatchRepository matchRepository,
                                             IStadiumRepository stadiumRepository,
                                             ISchedulingParametersRepository schedulingParametersRepository,
                                             ISchedulingDomainService schedulingDomainService)
    {
        private readonly ISchedulingDomainService _schedulingDomainService = schedulingDomainService;
        private readonly IMatchRepository _matchRepository = matchRepository;
        private readonly IStadiumRepository _stadiumRepository = stadiumRepository;
        private readonly ISchedulingParametersRepository _schedulingParametersRepository = schedulingParametersRepository;

        public List<(ConflictType, Guid, Guid?)> GetAllConflicts()
            => _schedulingDomainService.GetAllConflicts().Select(x => (x.Item1, x.Item2.Id, x.Item3?.Id)).ToList();

        public bool TeamsOfMatchesIsInConflict(MatchDto matchDto1,
                                               MatchDto matchDto2,
                                               bool checkWithRestTime = false)
        {
            var match1 = _matchRepository.GetById(matchDto1.Id ?? Guid.Empty).OrThrow();
            var match2 = _matchRepository.GetById(matchDto2.Id ?? Guid.Empty).OrThrow();
            var period1 = new Period(matchDto1.Date.ToUniversalTime(), matchDto1.Date.ToUniversalTime().AddFluentTimeSpan(match1.Format.GetFullTime() + (checkWithRestTime ? _schedulingParametersRepository.GetByMatch(match1).RestTime : TimeSpan.Zero)));
            var period2 = new Period(matchDto2.Date.ToUniversalTime(), matchDto2.Date.ToUniversalTime().AddFluentTimeSpan(match2.Format.GetFullTime() + (checkWithRestTime ? _schedulingParametersRepository.GetByMatch(match2).RestTime : TimeSpan.Zero)));

            return period1.Intersect(period2) && (match2.Participate(match1.HomeTeam) || match2.Participate(match1.AwayTeam));
        }

        public bool StadiumOfMatchesIsInConflict(MatchDto matchDto1,
                                                 MatchDto matchDto2,
                                                 bool checkWithRotationTime = false)
        {
            var match1 = _matchRepository.GetById(matchDto1.Id ?? Guid.Empty).OrThrow();
            var match2 = _matchRepository.GetById(matchDto2.Id ?? Guid.Empty).OrThrow();
            var stadium1 = matchDto1.Stadium?.Id is not null ? _stadiumRepository.GetById(matchDto1.Stadium.Id.Value) : null;
            var stadium2 = matchDto2.Stadium?.Id is not null ? _stadiumRepository.GetById(matchDto2.Stadium.Id.Value) : null;
            var period1 = new Period(matchDto1.Date.ToUniversalTime(), matchDto1.Date.ToUniversalTime().AddFluentTimeSpan(match1.Format.GetFullTime() + (checkWithRotationTime ? _schedulingParametersRepository.GetByMatch(match1).RotationTime : TimeSpan.Zero)));
            var period2 = new Period(matchDto2.Date.ToUniversalTime(), matchDto2.Date.ToUniversalTime().AddFluentTimeSpan(match2.Format.GetFullTime() + (checkWithRotationTime ? _schedulingParametersRepository.GetByMatch(match2).RotationTime : TimeSpan.Zero)));

            return period1.Intersect(period2) && stadium1 is not null && stadium1.Equals(stadium2);
        }

        public AvailabilityCheck GetTeamsAvaibility(IEnumerable<Guid> teamIds,
                                                    Period period,
                                                    IEnumerable<Guid>? ignoredMatchIds = null)
        {
            var utcPeriod = new Period(period.Start.ToUniversalTime(), period.End.ToUniversalTime());
            return !_schedulingDomainService.TeamsAreAvailable(teamIds, utcPeriod, false, ignoredMatchIds)
                ? AvailabilityCheck.IsBusy
                : !_schedulingDomainService.TeamsAreAvailable(teamIds, utcPeriod, true, ignoredMatchIds)
                ? AvailabilityCheck.IsPartiallyBusy
                : AvailabilityCheck.IsAvailable;
        }

        public AvailabilityCheck GetStadiumAvaibility(Guid stadiumId,
                                                      Period period,
                                                      IEnumerable<Guid>? ignoredMatchIds = null)
        {
            var utcPeriod = new Period(period.Start.ToUniversalTime(), period.End.ToUniversalTime());
            return !_schedulingDomainService.StadiumIsAvailable(stadiumId, utcPeriod, false, ignoredMatchIds)
                ? AvailabilityCheck.IsBusy
                : !_schedulingDomainService.StadiumIsAvailable(stadiumId, utcPeriod, true, ignoredMatchIds)
                ? AvailabilityCheck.IsPartiallyBusy
                : AvailabilityCheck.IsAvailable;
        }
    }
}
