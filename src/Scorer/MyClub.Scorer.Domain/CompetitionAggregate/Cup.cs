// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class Cup : Knockout, ICompetition
    {
        public Cup() : this(SchedulingParameters.Default) { }

        public Cup(SchedulingParameters schedulingParameters) => SchedulingParameters = schedulingParameters;

        public MatchFormat MatchFormat { get; set; } = MatchFormat.NoDraw;

        public MatchRules MatchRules { get; set; } = MatchRules.Default;

        public SchedulingParameters SchedulingParameters { get; set; }

        public override MatchFormat ProvideFormat() => MatchFormat;

        public override MatchRules ProvideRules() => MatchRules;

        public override SchedulingParameters ProvideSchedulingParameters() => SchedulingParameters;
    }
}

