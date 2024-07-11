// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Factories
{
    public class MatchdaysByDayOfWeekBuilder : MatchdaysBuilder
    {
        public DateTime StartDate { get; set; } = DateTime.Today;

        public TimeSpan Time { get; set; } = new TimeSpan(15, 0, 0);

        public IEnumerable<DayOfWeek> DayOfWeeks { get; set; } = [DayOfWeek.Sunday];

        protected override DateTime GetStartDate() => StartDate.PreviousDay().BeginningOfDay();

        protected override DateTime? ComputeMatchdayDate(DateTime? previousDate, int matchdayIndex) => DayOfWeeks.Min(x => previousDate.GetValueOrDefault().Next(x)).BeginningOfDay().ToUtcDateTime(Time);

        protected override DateTime ComputeMatchDate(DateTime previousDate, MatchInformation currentMatch, IEnumerable<MatchInformation> allMatches)
            => currentMatch.MatchdayDate.GetValueOrDefault().ToUniversalTime();
    }
}

