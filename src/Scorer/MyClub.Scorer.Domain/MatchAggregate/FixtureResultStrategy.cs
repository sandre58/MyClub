// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MyClub.Domain.Enums;

namespace MyClub.Scorer.Domain.MatchAggregate
{
    public class FixtureResultStrategy : IFixtureResultStrategy
    {
        public static readonly FixtureResultStrategy Default = new();

        public ExtendedResult GetExtendedResultOf(Fixture fixture, Guid teamId)
        {
            if (!fixture.IsPlayed()) return ExtendedResult.None;

            var team1 = fixture.Team1.GetTeam();
            var team2 = fixture.Team2.GetTeam();

            if (team1 is null || team2 is null) return ExtendedResult.None;

            var matches = fixture.GetAllMatches().ToList();
            var result1 = matches.Select(x => x.GetExtendedResultOf(team1.Id));
            var result2 = matches.Select(x => x.GetExtendedResultOf(team2.Id));
            var wonByTeam1 = result1.Count(x => x == ExtendedResult.Won);
            var wonByTeam2 = result2.Count(x => x == ExtendedResult.Won);
            var wonAfterShootoutByTeam1 = result1.Count(x => x == ExtendedResult.WonAfterShootouts);
            var wonAfterShootoutByTeam2 = result2.Count(x => x == ExtendedResult.WonAfterShootouts);
            var score1 = matches.Sum(x => x.Home!.GetScore());
            var score2 = matches.Sum(x => x.Away!.GetScore());

            return wonByTeam1 > wonByTeam2 && team1.Id == teamId ? ExtendedResult.Won
                 : wonByTeam1 < wonByTeam2 && team1.Id == teamId ? ExtendedResult.Lost
                 : wonByTeam2 > wonByTeam1 && team2.Id == teamId ? ExtendedResult.Won
                 : wonByTeam2 < wonByTeam1 && team2.Id == teamId ? ExtendedResult.Lost
                 : score1 > score2 && team1.Id == teamId ? ExtendedResult.Won
                 : score1 < score2 && team1.Id == teamId ? ExtendedResult.Lost
                 : score2 > score1 && team2.Id == teamId ? ExtendedResult.Won
                 : score2 < score1 && team2.Id == teamId ? ExtendedResult.Lost
                 : wonAfterShootoutByTeam1 > wonAfterShootoutByTeam2 && team1.Id == teamId ? ExtendedResult.WonAfterShootouts
                 : wonAfterShootoutByTeam1 < wonAfterShootoutByTeam2 && team1.Id == teamId ? ExtendedResult.LostAfterShootouts
                 : wonAfterShootoutByTeam2 > wonAfterShootoutByTeam1 && team2.Id == teamId ? ExtendedResult.WonAfterShootouts
                 : wonAfterShootoutByTeam2 < wonAfterShootoutByTeam1 && team2.Id == teamId ? ExtendedResult.LostAfterShootouts
                 : ExtendedResult.Drawn;

        }
        public Result GetResultOf(Fixture fixture, Guid teamId)
            => GetExtendedResultOf(fixture, teamId) switch
            {
                ExtendedResult.Won or ExtendedResult.WonAfterShootouts => Result.Won,
                ExtendedResult.Drawn => Result.Drawn,
                ExtendedResult.Lost or ExtendedResult.Withdrawn or ExtendedResult.LostAfterShootouts => Result.Lost,
                _ => Result.None,
            };
    }
}
