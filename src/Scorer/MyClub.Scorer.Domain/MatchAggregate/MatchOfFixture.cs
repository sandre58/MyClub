// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;

namespace MyClub.Scorer.Domain.MatchAggregate
{
    public class MatchOfFixture : MatchOfRound
    {
        public MatchOfFixture(Fixture fixture, DateTime date, MatchFormat? matchFormat = null, bool invertTeams = false, Guid? id = null)
            : base(fixture.Stage, date, !invertTeams ? fixture.Team1 : fixture.Team2, !invertTeams ? fixture.Team2 : fixture.Team1, matchFormat ?? MatchFormat.NoDraw, id) => Fixture = fixture;

        public Fixture Fixture { get; }
    }
}
