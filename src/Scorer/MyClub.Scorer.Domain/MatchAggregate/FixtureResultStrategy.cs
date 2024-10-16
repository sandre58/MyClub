// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.MatchAggregate
{
    public class FixtureResultStrategy : IFixtureResultStrategy
    {
        public static readonly FixtureResultStrategy Default = new();

        public ExtendedResult GetExtendedResultOf(IEnumerable<Match> matches, Team team, Team against)
        {
            var availableMatches = matches.Where(x => x.State is not MatchState.Cancelled).ToList();
            if (availableMatches.Any(x => !x.HasResult())) return ExtendedResult.None;

            var result1 = availableMatches.Select(x => x.GetExtendedResultOf(team.Id));
            var result2 = availableMatches.Select(x => x.GetExtendedResultOf(against.Id));
            var wonByTeam1 = result1.Count(x => x == ExtendedResult.Won);
            var wonByTeam2 = result2.Count(x => x == ExtendedResult.Won);
            var wonAfterShootoutByTeam1 = result1.Count(x => x == ExtendedResult.WonAfterShootouts);
            var wonAfterShootoutByTeam2 = result2.Count(x => x == ExtendedResult.WonAfterShootouts);
            var score1 = matches.Sum(x => x.GoalsFor(team.Id));
            var score2 = matches.Sum(x => x.GoalsFor(against.Id));
            var shootout1 = matches.Sum(x => x.ShootoutFor(team.Id));
            var shootout2 = matches.Sum(x => x.ShootoutFor(against.Id));

            return wonByTeam1 > wonByTeam2 ? ExtendedResult.Won
                 : wonByTeam1 < wonByTeam2 ? ExtendedResult.Lost
                 : score1 > score2 ? ExtendedResult.Won
                 : score1 < score2 ? ExtendedResult.Lost
                 : wonAfterShootoutByTeam1 > wonAfterShootoutByTeam2 ? ExtendedResult.WonAfterShootouts
                 : wonAfterShootoutByTeam1 < wonAfterShootoutByTeam2 ? ExtendedResult.LostAfterShootouts
                 : shootout1 > shootout2 ? ExtendedResult.WonAfterShootouts
                 : shootout1 < shootout2 ? ExtendedResult.LostAfterShootouts
                 : ExtendedResult.Drawn;

        }

        public Result GetResultOf(IEnumerable<Match> matches, Team team, Team against)
            => GetExtendedResultOf(matches, team, against) switch
            {
                ExtendedResult.Won or ExtendedResult.WonAfterShootouts => Result.Won,
                ExtendedResult.Drawn => Result.Drawn,
                ExtendedResult.Lost or ExtendedResult.Withdrawn or ExtendedResult.LostAfterShootouts => Result.Lost,
                _ => Result.None,
            };
    }
}
