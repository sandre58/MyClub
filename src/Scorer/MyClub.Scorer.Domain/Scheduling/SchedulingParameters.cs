// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Scheduling
{
    public class SchedulingParameters : ValueObject
    {
        public static readonly SchedulingParameters Default = new();

        public SchedulingParameters()
            : this(DateTime.Today.BeginningOfYear(), DateTime.Today.EndOfYear(), 15.Hours(), 1.Days(), 2.Days(), true) { }

        public SchedulingParameters(DateTime startDate,
                                    DateTime endDate,
                                    TimeSpan matchStartTime,
                                    TimeSpan rotationTime,
                                    TimeSpan minimumRestTime,
                                    bool useTeamVenues)
        {
            StartDate = startDate;
            EndDate = endDate;
            StartTime = matchStartTime;
            RotationTime = rotationTime;
            RestTime = minimumRestTime;
            UseTeamVenues = useTeamVenues;
        }

        public DateTime StartDate { get; }

        public DateTime EndDate { get; }

        public TimeSpan StartTime { get; }

        public TimeSpan RotationTime { get; }

        public TimeSpan RestTime { get; }

        public bool UseTeamVenues { get; }
    }
}
