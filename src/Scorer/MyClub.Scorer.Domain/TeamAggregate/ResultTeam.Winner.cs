// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;

namespace MyClub.Scorer.Domain.TeamAggregate
{
    public class WinnerTeam<T> : ResultTeam<T> where T : IFixture
    {
        public WinnerTeam(T fixture) : base(fixture, Result.Won) { }

        public override string ToString() => $"Winner of {Fixture}";
    }
}
