// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using MyClub.Domain.Enums;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.RankingAggregate;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.UI.Collections;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class RankingRowViewModel : ObservableObject, IIdentifiable<Guid>
    {
        private readonly UiObservableCollection<MatchOppositionViewModel> _lastMatches = [];
        private readonly UiObservableCollection<MatchOppositionViewModel> _matches = [];
        private readonly RankingViewModel _ranking;

        public RankingRowViewModel(RankingViewModel ranking, ITeamViewModel team)
        {
            Team = team;
            _ranking = ranking;
            LastMatches = new(_lastMatches);
            Matches = new(_matches);
        }

        public Guid Id => Team.Id;

        public ITeamViewModel Team { get; }

        public RankLabel? Label { get; private set; }

        public int Rank { get; private set; } = 1;

        public int? Progression { get; private set; }

        public int Points { get; private set; }

        public int Played { get; private set; }

        public int GamesDrawn { get; private set; }

        public int GamesLost { get; private set; }

        public int GamesWon { get; private set; }

        public int GamesWithdrawn { get; private set; }

        public int GamesWonAfterShootouts { get; private set; }

        public int GamesLostAfterShootouts { get; private set; }

        public int GoalsDifference { get; private set; }

        public int GoalsAgainst { get; private set; }

        public int GoalsFor { get; private set; }

        public ReadOnlyObservableCollection<MatchOppositionViewModel> LastMatches { get; }

        public ReadOnlyObservableCollection<MatchOppositionViewModel> Matches { get; }

        public void Update(RankingRowDto row, IEnumerable<MatchViewModel> matches)
        {
            Progression = row.Progression;
            Rank = row.Rank;
            Label = row.Label;
            Points = row.Points;
            Played = (int?)row.Columns?.GetOrDefault(DefaultRankingColumn.GamesPlayed.ToString()) ?? 0;
            GoalsDifference = (int?)row.Columns?.GetOrDefault(DefaultRankingColumn.GoalsDifference.ToString()) ?? 0;
            GamesDrawn = (int?)row.Columns?.GetOrDefault(DefaultRankingColumn.GamesDrawn.ToString()) ?? 0;
            GamesLost = (int?)row.Columns?.GetOrDefault(DefaultRankingColumn.GamesLost.ToString()) ?? 0;
            GamesLostAfterShootouts = (int?)row.Columns?.GetOrDefault(DefaultRankingColumn.GamesLostAfterShootouts.ToString()) ?? 0;
            GamesWithdrawn = (int?)row.Columns?.GetOrDefault(DefaultRankingColumn.GamesWithdrawn.ToString()) ?? 0;
            GamesWon = (int?)row.Columns?.GetOrDefault(DefaultRankingColumn.GamesWon.ToString()) ?? 0;
            GamesWonAfterShootouts = (int?)row.Columns?.GetOrDefault(DefaultRankingColumn.GamesWonAfterShootouts.ToString()) ?? 0;
            GoalsAgainst = (int?)row.Columns?.GetOrDefault(DefaultRankingColumn.GoalsAgainst.ToString()) ?? 0;
            GoalsFor = (int?)row.Columns?.GetOrDefault(DefaultRankingColumn.GoalsFor.ToString()) ?? 0;

            _matches.Set(matches.OrderBy(x => x.Date).Select(x => new MatchOppositionViewModel(Team, x)).ToList());
            _lastMatches.Set(_matches.TakeLast(_ranking.FormCount));
        }
        public void Reset()
        {
            Progression = null;
            Rank = 1;
            Played = 0;
            GoalsDifference = 0;
            Points = 0;
            GamesDrawn = 0;
            GamesLost = 0;
            GamesWon = 0;
            GamesWithdrawn = 0;
            GoalsAgainst = 0;
            GoalsFor = 0;
            GamesWonAfterShootouts = 0;
            GamesLostAfterShootouts = 0;

            _matches.Clear();
            _lastMatches.Clear();
        }

        public override string ToString()
        {
            var str = new StringBuilder($"{Rank}" +
                $" | {Team}" +
                $" | {Played} PLD" +
                $" | {Points} PTS" +
                $" | {GamesWon} W" +
                $" | {GamesDrawn} D" +
                $" | {GamesLost} L" +
                $" | {GoalsFor} GF" +
                $" | {GoalsAgainst} GA" +
                $" | {GoalsDifference} GD");

            return str.ToString();
        }
    }

    internal class MatchOppositionViewModel : ObservableObject
    {
        public ITeamViewModel Team { get; }

        public ITeamViewModel? Opponent { get; }

        public int GoalsFor { get; }

        public int GoalsAgainst { get; }

        public MatchResult Result { get; }

        public MatchResultDetailled ResultDetailled { get; }

        public MatchViewModel Match { get; }

        public MatchOppositionViewModel(ITeamViewModel team, MatchViewModel match)
        {
            Match = match;
            Team = team;
            Opponent = match.GetOpponentOf(team);
            GoalsFor = match.GoalsFor(team);
            GoalsAgainst = match.GoalsAgainst(team);
            Result = match.GetResultOf(team);
            ResultDetailled = match.GetDetailledResultOf(team);
        }
    }
}
