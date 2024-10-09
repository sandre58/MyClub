// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.BracketComputing
{
    public interface IRoundsAlgorithm
    {
        IEnumerable<BracketRound> Compute(IEnumerable<IVirtualTeam> teams);

        int NumberOfRounds(int teamsCount);
    }

    public class BracketRound(IEnumerable<BracketFixture> fixtures, IEnumerable<BracketTeam> teams)
    {
        public IEnumerable<BracketFixture> Fixtures { get; } = fixtures;

        public IEnumerable<BracketTeam> Teams { get; } = teams;
    }

    public class BracketFixture(BracketTeam team1, BracketTeam team2)
    {
        public BracketTeam Team1 { get; } = team1;

        public BracketTeam Team2 { get; } = team2;
    }

    public readonly struct BracketTeam
    {
        public BracketTeamType Type { get; }

        public IVirtualTeam? Team { get; }

        public BracketFixture? Fixture { get; }

        public BracketTeam(IVirtualTeam team) : this(BracketTeamType.Team, team, null) { }

        public BracketTeam(BracketFixture fixture, bool isWinner) : this(isWinner ? BracketTeamType.Winner : BracketTeamType.Looser, null, fixture) { }

        private BracketTeam(BracketTeamType type, IVirtualTeam? team, BracketFixture? fixture)
        {
            Type = type;
            Team = team;
            Fixture = fixture;
        }
    }

    public enum BracketTeamType
    {
        Team,

        Winner,

        Looser
    }
}
