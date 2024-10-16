// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.BracketComputing
{
    public class KnockoutAlgorithm : IRoundsAlgorithm
    {
        public static KnockoutAlgorithm Default => new();

        public int NumberOfRounds(int teamsCount) => (int)Math.Ceiling(Math.Log2(teamsCount));

        public IEnumerable<BracketRound> Compute(IEnumerable<IVirtualTeam> teams)
        {
            var numberOfTeams = teams.Count();

            if (numberOfTeams <= 1) return [];

            var numberOfRounds = NumberOfRounds(numberOfTeams);
            var numberOfFixturesForFirstRound = numberOfTeams - Math.Pow(2, numberOfRounds - 1);
            var remainingTeams = new Stack<BracketTeam>(teams.Select(x => new BracketTeam(x)));
            var result = new List<BracketRound>();

            for (var roundIndex = numberOfRounds; roundIndex >= 1; roundIndex--)
            {
                var fixtures = new List<BracketFixture>();
                var teamsForNextRound = new List<BracketTeam>();
                var teamsForCurrentRound = remainingTeams.ToList();
                var numberOfFixtures = roundIndex == numberOfRounds ? numberOfFixturesForFirstRound : Math.Pow(2, roundIndex - 1);

                for (var fixtureIndex = 0; fixtureIndex < numberOfFixtures; fixtureIndex++)
                {
                    var team1 = remainingTeams.Pop();
                    var team2 = remainingTeams.Pop();
                    var fixture = new BracketFixture(team1, team2);
                    fixtures.Add(fixture);
                    teamsForNextRound.Add(new(fixture, true));
                }
                teamsForNextRound.InsertRange(0, remainingTeams);

                result.Add(new(fixtures, teamsForCurrentRound));

                remainingTeams = new(teamsForNextRound);
            }

            return result;
        }
    }
}

