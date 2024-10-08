﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class SchedulingParameters : ValueObject
    {
        public static readonly SchedulingParameters Default = new();

        public SchedulingParameters()
            : this(DateTime.UtcNow.ToDate(), DateTime.UtcNow.AddYears(1).ToDate(), 15.Hours().ToTime(), 1.Days(), 2.Days(), true, false, 1.Days(), true, [], [new IncludeDaysOfWeekRule([DayOfWeek.Saturday])], [], []) { }

        public SchedulingParameters(DateOnly startDate,
                                    DateOnly endDate,
                                    TimeOnly matchStartTime,
                                    TimeSpan rotationTime,
                                    TimeSpan minimumRestTime,
                                    bool useHomeVenue,
                                    bool asSoonAsPossible,
                                    TimeSpan interval,
                                    bool scheduleByStage,
                                    IEnumerable<IAvailableDateSchedulingRule> asSoonAsPossibleRules,
                                    IEnumerable<IDateSchedulingRule> dateRules,
                                    IEnumerable<ITimeSchedulingRule> timeRules,
                                    IEnumerable<IAvailableVenueSchedulingRule> venueRules)
        {
            StartDate = startDate;
            EndDate = endDate;
            StartTime = matchStartTime;
            RotationTime = rotationTime;
            RestTime = minimumRestTime;
            UseHomeVenue = useHomeVenue;
            AsSoonAsPossible = asSoonAsPossible;
            Interval = interval;
            ScheduleByStage = scheduleByStage;
            VenueRules = new List<IAvailableVenueSchedulingRule>(venueRules).AsReadOnly();
            AsSoonAsPossibleRules = new List<IAvailableDateSchedulingRule>(asSoonAsPossibleRules).AsReadOnly();
            DateRules = new List<IDateSchedulingRule>(dateRules).AsReadOnly();
            TimeRules = new List<ITimeSchedulingRule>(timeRules).AsReadOnly();
        }

        public DateOnly StartDate { get; }

        public DateOnly EndDate { get; }

        public TimeOnly StartTime { get; }

        public TimeSpan RotationTime { get; }

        public TimeSpan RestTime { get; }

        public bool UseHomeVenue { get; }

        public bool AsSoonAsPossible { get; }

        public TimeSpan Interval { get; }

        public bool ScheduleByStage { get; }

        public IReadOnlyCollection<IAvailableVenueSchedulingRule> VenueRules { get; }

        public IReadOnlyCollection<IAvailableDateSchedulingRule> AsSoonAsPossibleRules { get; }

        public IReadOnlyCollection<IDateSchedulingRule> DateRules { get; }

        public IReadOnlyCollection<ITimeSchedulingRule> TimeRules { get; }

        public DateTime Start() => StartDate.At(StartTime, DateTimeKind.Utc);
    }
}
