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
            var result = new List<BracketRound>();
            var teamsForCurrentRound = teams.Select(x => new BracketTeam(x)).ToList();

            while (teamsForCurrentRound.Count > 1)
            {
                var teamsForNextRound = new List<BracketTeam>();
                var fixtures = new List<BracketFixture>();
                for (var i = 0; i < teamsForCurrentRound.Count; i += 2)
                {
                    if (i + 1 < teamsForCurrentRound.Count)
                    {
                        var fixture = new BracketFixture(teamsForCurrentRound[i], teamsForCurrentRound[i + 1]);
                        fixtures.Add(fixture);
                        teamsForNextRound.Add(new(fixture, true));
                    }
                    else
                    {
                        // Handle the case where there is an odd number of teams
                        // The last team automatically advances to the next round
                        teamsForNextRound.Add(teamsForCurrentRound[i]);
                    }
                }
                result.Add(new(fixtures, teamsForCurrentRound.ToList()));

                teamsForCurrentRound = [.. teamsForNextRound];
            }

            return result;
        }
    }
}

