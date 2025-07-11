// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.BracketComputing
{
    public class ByeTeamAlgorithm : IRoundsAlgorithm
    {
        public static ByeTeamAlgorithm Default => new();

        public int NumberOfRounds(int teamsCount) => (int)Math.Ceiling(Math.Log2(teamsCount));

        public BracketRound Compute(IEnumerable<IVirtualTeam> teams)
        {
            var numberOfTeams = teams.Count();

            if (numberOfTeams <= 1) throw new InvalidOperationException("Teams count must be greater than 1");

            // FirstRound
            var remainingTeams = new Stack<BracketTeam>(teams.Select(x => new BracketTeam(x)));
            var fixtures = new List<BracketFixture>();
            while (remainingTeams.Count >= 2)
            {
                var team1 = remainingTeams.Pop();
                var team2 = remainingTeams.Pop();
                var fixture = new BracketFixture(team1, team2);
                fixtures.Add(fixture);
            }

            return new BracketRound(fixtures, fixtures.Count > 1 ? KnockoutAlgorithmHelper.ComputeTree(fixtures, remainingTeams) : []);
        }
    }
}

