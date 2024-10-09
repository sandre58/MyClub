// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;

namespace MyClub.Scorer.Domain.TeamAggregate
{
    public class LooserTeam<T> : ResultTeam<T> where T : IFixture
    {
        public LooserTeam(T fixture) : base(fixture, Result.Lost) { }

        public override string ToString() => $"Looser of {Fixture}";
    }
}
