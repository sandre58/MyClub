// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;

namespace MyClub.Scorer.Domain.Extensions
{
    public static class TeamExtensions
    {
        public static bool IsAvailable(this Team team, IEnumerable<Match> matches, Period period, bool withRestTime = false)
            => !matches.Any(x => x.State != MatchState.Cancelled && x.Participate(team) && period.IntersectWith(withRestTime ? new Period(x.Date.SubtractFluentTimeSpan(x.GetRestTime()), x.Date.AddFluentTimeSpan(x.Format.GetFullTime() + x.GetRestTime())) : x.GetPeriod()));
    }
}
