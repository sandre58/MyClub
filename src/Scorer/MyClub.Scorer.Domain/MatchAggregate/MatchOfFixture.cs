// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Domain.CompetitionAggregate;

namespace MyClub.Scorer.Domain.MatchAggregate
{
    public class MatchOfFixture : Match
    {
        public MatchOfFixture(Fixture fixture, RoundStage roundStage, DateTime date, bool invertTeams = false, Guid? id = null)
            : base(date, !invertTeams ? fixture.Team1 : fixture.Team2, !invertTeams ? fixture.Team2 : fixture.Team1, id: id)
        {
            Fixture = fixture;
            Stage = roundStage;
        }

        public Fixture Fixture { get; }

        public RoundStage Stage { get; }

        public override MatchFormat Format => Stage.ProvideFormat();

        public override MatchRules Rules => Stage.ProvideRules();

        public override bool UseExtraTime() => Format.ExtraTimeIsEnabled && Fixture.Stage.Format.MustUseExtraTime(this);

        public override bool UseShootout() => Format.ShootoutIsEnabled && Fixture.Stage.Format.MustUseShootout(this);
    }
}
