// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Scorer.Domain.MatchAggregate;

namespace MyClub.Scorer.Domain.Scheduling
{
    public interface IMatchesScheduler
    {
        void Schedule(IEnumerable<SchedulingMatchInformation> matches);
    }

    public readonly struct SchedulingMatchInformation
    {
        public SchedulingMatchInformation(Match match, int index, int parentIndex, SchedulingParameters schedulingParameters)
        {
            Match = match;
            Index = index;
            ParentIndex = parentIndex;
            SchedulingParameters = schedulingParameters;
        }

        public Match Match { get; }

        public int Index { get; }

        public int ParentIndex { get; }

        public DateTime ParentDate { get; }

        public SchedulingParameters SchedulingParameters { get; }
    }
}

