// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyNet.Observable;
using MyNet.UI.Collections;
using MyNet.Utilities;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class RankingRowViewModel : ObservableObject, IIdentifiable<Guid>
    {
        private readonly UiObservableCollection<MatchViewModel> _lastMatches = [];

        public RankingRowViewModel(TeamViewModel team)
        {
            Team = team;
            LastMatches = new(_lastMatches);
        }

        public Guid Id { get; } = Guid.NewGuid();

        public TeamViewModel Team { get; }

        public int Rank { get; private set; } = 1;

        public int Played { get; private set; }

        public int Penalties { get; private set; }

        public int GoalDifference { get; private set; }

        public int Points { get; private set; }

        public int GamesDrawn { get; private set; }

        public int GamesLost { get; private set; }

        public int GamesWithdrawn { get; private set; }

        public int GamesWon { get; private set; }

        public int GoalsAgainst { get; private set; }

        public int GoalsFor { get; private set; }

        public ReadOnlyObservableCollection<MatchViewModel> LastMatches { get; }

        public void Update(RankingRow row, int rank, IEnumerable<MatchViewModel> allMatches, int formCount = 5)
        {
            Rank = rank;
            Played = row.Get(RankingColumn.Played);
            Penalties = row.Get(RankingColumn.PenaltyPoints);
            GoalDifference = row.Get(RankingColumn.GoalsDifference);
            Points = row.Get(RankingColumn.Points);
            GamesDrawn = row.Get(RankingColumn.GamesDrawn);
            GamesLost = row.Get(RankingColumn.GamesLost);
            GamesWithdrawn = row.Get(RankingColumn.GamesWithdrawn);
            GamesWon = row.Get(RankingColumn.GamesWon);
            GoalsAgainst = row.Get(RankingColumn.GoalsAgainst);
            GoalsFor = row.Get(RankingColumn.GoalsFor);

            _lastMatches.Set(row.Matches.TakeLast(formCount).Select(x => allMatches.GetByIdOrDefault(x.Id)).NotNull().ToList());
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
                $" | {GamesWithdrawn} WD" +
                $" | {Penalties} P" +
                $" | {GoalsFor} GF" +
                $" | {GoalsAgainst} GA" +
                $" | {GoalDifference} GD");

            return str.ToString();
        }
    }
}
