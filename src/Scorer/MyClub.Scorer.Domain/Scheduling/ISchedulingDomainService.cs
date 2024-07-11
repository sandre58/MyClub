// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;
using MyNet.Utilities.DateTimes;

namespace MyClub.Scorer.Domain.Scheduling
{
    public interface ISchedulingDomainService
    {
        IEnumerable<(ConflictType, Match, Match?)> GetAllConflicts();

        IEnumerable<ConflictType> GetConflictsBetween(Match match1, Match match2);

        bool TeamsAreAvailable(IEnumerable<Guid> teamIds, Period period, bool withRestTime = false, IEnumerable<Guid>? ignoredMatchIds = null);

        bool StadiumIsAvailable(Guid stadiumId, Period period, bool withRotationTime = false, IEnumerable<Guid>? ignoredMatchIds = null);
    }
}
