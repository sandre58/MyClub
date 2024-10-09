// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.Extensions
{
    public static class FixtureExtensions
    {
        public static Result GetResultOf(this IFixture fixture, Team team) => fixture.GetResultOf(team.Id);

        public static bool IsWonBy(this IFixture fixture, Team team) => fixture.IsWonBy(team.Id);

        public static bool IsWonBy(this IFixture fixture, Guid teamId) => fixture.GetResultOf(teamId) == Result.Won;

        public static bool IsLostBy(this IFixture fixture, Team team) => fixture.IsLostBy(team.Id);

        public static bool IsLostBy(this IFixture fixture, Guid teamId) => fixture.GetResultOf(teamId) == Result.Lost;

        public static bool Participate(this IFixture fixture, IVirtualTeam team) => fixture.Participate(team.Id);

        public static T? Find<T>(this IEnumerable<T> fixtures, IVirtualTeam team1, IVirtualTeam team2) where T : IFixture => fixtures.FirstOrDefault(x => x.Participate(team1) && x.Participate(team2));

        public static IEnumerable<T> FindAll<T>(this IEnumerable<T> fixtures, IVirtualTeam team1, IVirtualTeam team2) where T : IFixture => fixtures.Where(x => x.Participate(team1) && x.Participate(team2));
    }
}
