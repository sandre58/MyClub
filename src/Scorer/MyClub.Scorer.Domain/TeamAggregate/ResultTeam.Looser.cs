// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;

namespace MyClub.Scorer.Domain.TeamAggregate
{
    public class LooserOfMatchTeam : LooserTeam<Match>
    {
        public LooserOfMatchTeam(Match fixture) : base(fixture)
        {
        }

        public override string ToString() => $"Looser of [{Fixture.HomeTeam}] vs [{Fixture.AwayTeam}]";
    }

    public class LooserOfFixtureTeam : LooserTeam<Fixture>
    {
        public LooserOfFixtureTeam(Fixture fixture) : base(fixture)
        {
        }

        public override string ToString() => $"Looser of [{Fixture.Team1}] vs [{Fixture.Team2}]";
    }

    public class LooserTeam<T> : ResultTeam<T> where T : IFixture
    {
        public LooserTeam(T fixture) : base(fixture, Result.Lost) { }

        public override string ToString() => $"Looser of {Fixture}";
    }
}
