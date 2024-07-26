// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;

namespace MyClub.Scorer.Domain.Extensions
{
    public static class StadiumExtensions
    {
        public static bool IsAvailable(this Stadium stadium, IEnumerable<Match> matches, Period period, bool withRotationTime = false)
            => !matches.Any(x => x.State != MatchState.Cancelled && x.Stadium is not null && x.Stadium == stadium && period.IntersectWith(withRotationTime ? new Period(x.Date.SubtractFluentTimeSpan(x.GetRotationTime()), x.Date.AddFluentTimeSpan(x.Format.GetFullTime() + x.GetRotationTime())) : x.GetPeriod()));
    }
}
