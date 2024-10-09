// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyNet.Utilities;
using MyNet.Utilities.Extensions;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class KnockoutStage : Knockout, ITournamentStage
    {
        private string _shortName = string.Empty;
        private string _name = string.Empty;

        public KnockoutStage(IStage stage, string name, string? shortName = null, MatchFormat? matchFormat = null, MatchRules? matchRules = null, SchedulingParameters? schedulingParameters = null, Guid? id = null) : base(id)
        {
            Stage = stage;
            Name = name;
            ShortName = shortName ?? name.GetInitials();
            MatchFormat = matchFormat ?? stage.ProvideFormat();
            MatchRules = matchRules ?? stage.ProvideRules();
            SchedulingParameters = schedulingParameters ?? stage.ProvideSchedulingParameters();
        }

        public string Name
        {
            get => _name;
            set => _name = value.IsRequiredOrThrow();
        }

        public string ShortName
        {
            get => _shortName;
            set => _shortName = value.IsRequiredOrThrow();
        }

        public IStage Stage { get; }

        public MatchFormat MatchFormat { get; set; }

        public MatchRules MatchRules { get; set; }

        public SchedulingParameters SchedulingParameters { get; set; }

        public override MatchFormat ProvideFormat() => MatchFormat;

        public override MatchRules ProvideRules() => MatchRules;

        public override SchedulingParameters ProvideSchedulingParameters() => SchedulingParameters;
    }
}
