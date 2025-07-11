// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Extensions;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class KnockoutStage : Knockout, ITournamentStage
    {
        private string _shortName = string.Empty;
        private string _name = string.Empty;

        public KnockoutStage(Tournament stage, string name, string? shortName = null, MatchFormat? matchFormat = null, MatchRules? matchRules = null, SchedulingParameters? schedulingParameters = null, Guid? id = null)
            : base(matchFormat, matchRules, schedulingParameters, id)
        {
            Stage = stage;
            Name = name;
            ShortName = shortName ?? name.GetInitials();
        }

        public Tournament Stage { get; }

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

        public MatchRules ProvideRules() => MatchRules;

        public SchedulingParameters ProvideSchedulingParameters() => SchedulingParameters;

        public IEnumerable<IVirtualTeam> ProvideTeams() => Teams.AsEnumerable();
    }
}
