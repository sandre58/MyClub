// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.BracketComputing
{
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
}
