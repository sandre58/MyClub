// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Extensions;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class Matchday : MatchesStage<MatchOfMatchday>
    {
        private string _name = string.Empty;
        private string _shortName = string.Empty;

        public Matchday(IMatchdaysStage stage, DateTime date, string name, string? shortName = null, Guid? id = null) : base(date, id)
        {
            Stage = stage;
            Name = name;
            ShortName = shortName ?? name.GetInitials();
        }

        public IMatchdaysStage Stage { get; }

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

        public override MatchFormat ProvideFormat() => Stage.ProvideFormat();

        public override MatchRules ProvideRules() => Stage.ProvideRules();

        public override SchedulingParameters ProvideSchedulingParameters() => Stage.ProvideSchedulingParameters();

        protected override MatchOfMatchday Create(DateTime date, IVirtualTeam homeTeam, IVirtualTeam awayTeam) => new(this, date, homeTeam, awayTeam);

        public override MatchOfMatchday AddMatch(MatchOfMatchday match)
            => !ReferenceEquals(match.Stage, this)
                ? throw new ArgumentException("Match stage is not this matchday", nameof(match))
                : base.AddMatch(match);

        public override string ToString() => Name;
    }
}
