// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Scorer.Domain.Scheduling;

namespace MyClub.Scorer.Application.Dtos
{
    public class NewMatchdaysDto
    {
        public Guid? ParentId { get; set; }

        public AddMatchdaysDatesParametersDto? DatesParameters { get; set; }

        public string? NamePattern { get; set; }

        public string? ShortNamePattern { get; set; }

        public bool ScheduleVenues { get; set; }

        public int StartIndex { get; set; }

        public Guid? StartDuplicatedMatchday { get; set; }

        public bool InvertTeams { get; set; }

        public TimeOnly StartTime { get; set; }
    }

    public abstract class AddMatchdaysDatesParametersDto { }

    public class AddMatchdaysAutomaticDatesParametersDto : AddMatchdaysDatesParametersDto
    {
        public DateOnly StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        public int? Number { get; set; }

        public List<IDateSchedulingRule>? DateRules { get; set; }

        public List<ITimeSchedulingRule>? TimeRules { get; set; }
    }

    public class AddMatchdaysManualDatesParametersDto : AddMatchdaysDatesParametersDto
    {
        public List<DateTime>? Dates { get; set; }
    }
}
