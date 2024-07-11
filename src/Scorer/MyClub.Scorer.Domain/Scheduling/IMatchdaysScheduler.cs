// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Scorer.Domain.CompetitionAggregate;

namespace MyClub.Scorer.Domain.Scheduling
{
    public interface IMatchdaysScheduler
    {
        void Schedule(IEnumerable<SchedulingMatchdayInformation> matchdays);
    }

    public readonly struct SchedulingMatchdayInformation
    {
        public SchedulingMatchdayInformation(Matchday matchday, int index, SchedulingParameters schedulingParameters)
        {
            Matchday = matchday;
            Index = index;
            SchedulingParameters = schedulingParameters;
        }

        public Matchday Matchday { get; }

        public int Index { get; }

        public SchedulingParameters SchedulingParameters { get; }
    }
}

