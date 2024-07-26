// Copyright (c) Stéphane ANDRE. All Right Reserved.
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
            : this(DateTime.Today.BeginningOfYear(), DateTime.Today.EndOfYear(), 15.Hours(), 1.Days(), 2.Days(), true, false, 1.Days(), true, [], [], [], []) { }

        public SchedulingParameters(DateTime startDate,
                                    DateTime endDate,
                                    TimeSpan matchStartTime,
                                    TimeSpan rotationTime,
                                    TimeSpan minimumRestTime,
                                    bool useHomeVenue,
                                    bool asSoonAsPossible,
                                    TimeSpan interval,
                                    bool scheduleByParent,
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
            ScheduleByParent = scheduleByParent;
            VenueRules = new List<IAvailableVenueSchedulingRule>(venueRules).AsReadOnly();
            AsSoonAsPossibleRules = new List<IAvailableDateSchedulingRule>(asSoonAsPossibleRules).AsReadOnly();
            DateRules = new List<IDateSchedulingRule>(dateRules).AsReadOnly();
            TimeRules = new List<ITimeSchedulingRule>(timeRules).AsReadOnly();
        }

        public DateTime StartDate { get; }

        public DateTime EndDate { get; }

        public TimeSpan StartTime { get; }

        public TimeSpan RotationTime { get; }

        public TimeSpan RestTime { get; }

        public bool UseHomeVenue { get; }

        public bool AsSoonAsPossible { get; }

        public TimeSpan Interval { get; }

        public bool ScheduleByParent { get; }

        public IReadOnlyCollection<IAvailableVenueSchedulingRule> VenueRules { get; }

        public IReadOnlyCollection<IAvailableDateSchedulingRule> AsSoonAsPossibleRules { get; }

        public IReadOnlyCollection<IDateSchedulingRule> DateRules { get; }

        public IReadOnlyCollection<ITimeSchedulingRule> TimeRules { get; }
    }
}
