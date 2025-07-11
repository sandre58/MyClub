// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class Cup : Knockout, ICompetition
    {
        public Cup() : this(MatchFormat.Default, MatchRules.Default, SchedulingParameters.Default) { }

        public Cup(MatchFormat matchFormat, MatchRules matchRules, SchedulingParameters schedulingParameters, Guid? id = null)
            : base(matchFormat, matchRules, schedulingParameters, id) { }
    }
}

