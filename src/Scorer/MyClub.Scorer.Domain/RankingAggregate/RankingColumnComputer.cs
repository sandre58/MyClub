// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.RankingAggregate
{
    public abstract class RankingColumnComputer<T> : IRankingColumnComputer, IRankingColumnComputer<T>
    {
        public T Compute(RankingRow row) => Compute(row.Team, row.GetMatches());

        object IRankingColumnComputer.Compute(RankingRow row) => Compute(row)!;

        protected abstract T Compute(ITeam team, IEnumerable<Match> matches);
    }

    public class DefaultRankingColumnComputer<T> : RankingColumnComputer<T>
    {
        private readonly Func<IEnumerable<Match>, ITeam, T> _aggregate;

        public DefaultRankingColumnComputer(Func<IEnumerable<Match>, ITeam, T> aggregate) => _aggregate = aggregate;

        protected override T Compute(ITeam team, IEnumerable<Match> matches) => _aggregate.Invoke(matches, team);
    }

    public class PlayedColumnComputer : DefaultRankingColumnComputer<int>
    {
        public PlayedColumnComputer() : base((matches, team) => matches.Count(x => x.Participate(team))) { }
    }

    public class GamesWonColumnComputer : DefaultRankingColumnComputer<int>
    {
        public GamesWonColumnComputer() : base((matches, team) => matches.Count(x => x.GetDetailledResultOf(team) == MatchResultDetailled.Won)) { }
    }

    public class GamesDrawnColumnComputer : DefaultRankingColumnComputer<int>
    {
        public GamesDrawnColumnComputer() : base((matches, team) => matches.Count(x => x.GetDetailledResultOf(team) == MatchResultDetailled.Drawn)) { }
    }

    public class GamesLostColumnComputer : DefaultRankingColumnComputer<int>
    {
        public GamesLostColumnComputer() : base((matches, team) => matches.Count(x => x.GetDetailledResultOf(team) == MatchResultDetailled.Lost)) { }
    }

    public class GamesWithdrawnColumnComputer : DefaultRankingColumnComputer<int>
    {
        public GamesWithdrawnColumnComputer() : base((matches, team) => matches.Count(x => x.GetDetailledResultOf(team) == MatchResultDetailled.Withdrawn)) { }
    }

    public class GoalsForColumnComputer : DefaultRankingColumnComputer<int>
    {
        public GoalsForColumnComputer() : base((matches, team) => matches.Sum(x => x.GoalsFor(team))) { }
    }

    public class GoalsAgainstColumnComputer : DefaultRankingColumnComputer<int>
    {
        public GoalsAgainstColumnComputer() : base((matches, team) => matches.Sum(x => x.GoalsAgainst(team))) { }
    }

    public class GoalsDifferenceColumnComputer : DefaultRankingColumnComputer<int>
    {
        public GoalsDifferenceColumnComputer() : base((matches, team) => matches.Sum(x => x.GoalsFor(team) - x.GoalsAgainst(team))) { }
    }
}
