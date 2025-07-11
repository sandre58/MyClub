// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

namespace MyClub.Scorer.Domain.BracketComputing
{
    public class BracketFixture(BracketTeam team1, BracketTeam team2)
    {
        public BracketTeam Team1 { get; } = team1;

        public BracketTeam Team2 { get; } = team2;
    }
}
