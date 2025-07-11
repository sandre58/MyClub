// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace MyClub.Scorer.Domain.BracketComputing
{
    public static class KnockoutAlgorithmHelper
    {
        public static Dictionary<BracketType, BracketRound> ComputeTree(IEnumerable<BracketFixture> previousFixtures, IEnumerable<BracketTeam> remainingTeams)
        {
            var result = new Dictionary<BracketType, BracketRound>();

            var (winnerFixtures, teams) = ComputeRound(previousFixtures, true, remainingTeams);
            result.Add(BracketType.Winner, new BracketRound(winnerFixtures, winnerFixtures.Count + teams.Count() > 1 ? ComputeTree(winnerFixtures, teams) : []));

            if (remainingTeams.Count() < 2)
            {
                var (looserFixtures, teams1) = ComputeRound(previousFixtures, false, []);
                result.Add(BracketType.Looser, new BracketRound(looserFixtures, looserFixtures.Count + teams1.Count() > 1 ? ComputeTree(looserFixtures, teams1) : []));
            }

            return result;
        }

        public static (ICollection<BracketFixture> fixtures, IEnumerable<BracketTeam> remainingTeams) ComputeRound(IEnumerable<BracketFixture> previousFixtures, bool isWinnerRound, IEnumerable<BracketTeam> remainingTeams)
        {
            var teams = new Stack<BracketTeam>(previousFixtures.Select(x => new BracketTeam(x, isWinnerRound)).Union(remainingTeams));
            var fixtures = new List<BracketFixture>();
            var teamsForNextRound = new List<BracketTeam>();

            while (teams.Count >= 2)
            {
                var team1 = teams.Pop();
                var team2 = teams.Pop();
                var fixture = new BracketFixture(team1, team2);
                fixtures.Add(fixture);
            }

            return (fixtures, teams);
        }
    }
}

