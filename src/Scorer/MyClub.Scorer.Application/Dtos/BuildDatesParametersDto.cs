// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Scorer.Domain.Scheduling;
using MyNet.Utilities.Units;

namespace MyClub.Scorer.Application.Dtos
{
    public abstract class BuildDatesParametersDto { }

    public class BuildAsSoonAsPossibleDatesParametersDto : BuildDatesParametersDto
    {
        public DateTime? StartDate { get; set; }

        public List<IAvailableDateSchedulingRule>? Rules { get; set; }
    }

    public class BuildAutomaticDatesParametersDto : BuildDatesParametersDto
    {
        public DateOnly? StartDate { get; set; }

        public TimeOnly? DefaultTime { get; set; }

        public int IntervalValue { get; set; }

        public TimeUnit IntervalUnit { get; set; }

        public List<IDateSchedulingRule>? DateRules { get; set; }

        public List<ITimeSchedulingRule>? TimeRules { get; set; }
    }

    public class BuildManualDatesParametersDto : BuildDatesParametersDto
    {
        public List<(DateTime date, IEnumerable<DateTime> datesOfMatches)>? Dates { get; set; }
    }
}
