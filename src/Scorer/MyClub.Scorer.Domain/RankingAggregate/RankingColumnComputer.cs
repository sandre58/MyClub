// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.RankingAggregate
{
    public abstract class RankingColumnComputer<T> : IRankingColumnComputer, IRankingColumnComputer<T>
    {
        public T Compute(RankingRow row) => Compute(row.Team, row.GetMatches());

        object IRankingColumnComputer.Compute(RankingRow row) => Compute(row)!;

        protected abstract T Compute(IVirtualTeam team, IEnumerable<Match> matches);
    }

    public class DefaultRankingColumnComputer<T> : RankingColumnComputer<T>
    {
        private readonly Func<IEnumerable<Match>, IVirtualTeam, T> _aggregate;

        public DefaultRankingColumnComputer(Func<IEnumerable<Match>, IVirtualTeam, T> aggregate) => _aggregate = aggregate;

        protected override T Compute(IVirtualTeam team, IEnumerable<Match> matches) => _aggregate.Invoke(matches, team);
    }

    public class PlayedColumnComputer : DefaultRankingColumnComputer<int>
    {
        public PlayedColumnComputer() : base((matches, team) => matches.Count(x => x.Participate(team))) { }
    }

    public class GamesWonColumnComputer : DefaultRankingColumnComputer<int>
    {
        public GamesWonColumnComputer() : base((matches, team) => team.GetTeam() is Team team1 ? matches.Count(x => x.GetExtendedResultOf(team1) == ExtendedResult.Won) : 0) { }
    }

    public class GamesWonAfterShootoutsColumnComputer : DefaultRankingColumnComputer<int>
    {
        public GamesWonAfterShootoutsColumnComputer() : base((matches, team) => team.GetTeam() is Team team1 ? matches.Count(x => x.GetExtendedResultOf(team1) == ExtendedResult.WonAfterShootouts) : 0) { }
    }

    public class GamesDrawnColumnComputer : DefaultRankingColumnComputer<int>
    {
        public GamesDrawnColumnComputer() : base((matches, team) => team.GetTeam() is Team team1 ? matches.Count(x => x.GetExtendedResultOf(team1) == ExtendedResult.Drawn) : 0) { }
    }

    public class GamesLostColumnComputer : DefaultRankingColumnComputer<int>
    {
        public GamesLostColumnComputer() : base((matches, team) => team.GetTeam() is Team team1 ? matches.Count(x => x.GetExtendedResultOf(team1) == ExtendedResult.Lost) : 0) { }
    }

    public class GamesLostAfterShootoutsColumnComputer : DefaultRankingColumnComputer<int>
    {
        public GamesLostAfterShootoutsColumnComputer() : base((matches, team) => team.GetTeam() is Team team1 ? matches.Count(x => x.GetExtendedResultOf(team1) == ExtendedResult.LostAfterShootouts) : 0) { }
    }

    public class GamesWithdrawnColumnComputer : DefaultRankingColumnComputer<int>
    {
        public GamesWithdrawnColumnComputer() : base((matches, team) => team.GetTeam() is Team team1 ? matches.Count(x => x.GetExtendedResultOf(team1) == ExtendedResult.Withdrawn) : 0) { }
    }

    public class GoalsForColumnComputer : DefaultRankingColumnComputer<int>
    {
        public GoalsForColumnComputer() : base((matches, team) => team.GetTeam() is Team team1 ? matches.Sum(x => x.GoalsFor(team1)) : 0) { }
    }

    public class GoalsAgainstColumnComputer : DefaultRankingColumnComputer<int>
    {
        public GoalsAgainstColumnComputer() : base((matches, team) => team.GetTeam() is Team team1 ? matches.Sum(x => x.GoalsAgainst(team1)) : 0) { }
    }

    public class GoalsDifferenceColumnComputer : DefaultRankingColumnComputer<int>
    {
        public GoalsDifferenceColumnComputer() : base((matches, team) => team.GetTeam() is Team team1 ? matches.Sum(x => x.GoalsFor(team1) - x.GoalsAgainst(team1)) : 0) { }
    }
}
