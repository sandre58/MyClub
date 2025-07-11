// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.Extensions
{
    public static class FixtureExtensions
    {
        public static T? Find<T>(this IEnumerable<T> fixtures, IVirtualTeam team) where T : IFixture => fixtures.FirstOrDefault(x => x.Participate(team));

        public static T? Find<T>(this IEnumerable<T> fixtures, IVirtualTeam team1, IVirtualTeam team2) where T : IFixture => fixtures.FirstOrDefault(x => x.Participate(team1) && x.Participate(team2));

        public static IEnumerable<T> FindAll<T>(this IEnumerable<T> fixtures, IVirtualTeam team1, IVirtualTeam team2) where T : IFixture => fixtures.Where(x => x.Participate(team1) && x.Participate(team2));
    }
}
