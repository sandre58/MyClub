// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.List;
using MyNet.UI.ViewModels.List.Sorting;

namespace MyClub.Scorer.Wpf.ViewModels.RankingPage
{
    internal sealed class RankingListParameterProvider : ListParametersProvider
    {
        private readonly DisplayViewModel _display;

        public static RankingListParameterProvider Full => new([
                nameof(RankingRowViewModel.Rank),
                nameof(RankingRowViewModel.Progression),
                nameof(RankingRowViewModel.Label),
                nameof(RankingRowViewModel.Team),
                nameof(RankingRowViewModel.Points),
                nameof(RankingRowViewModel.Played),
                nameof(RankingRowViewModel.GamesWon),
                nameof(RankingRowViewModel.GamesDrawn),
                nameof(RankingRowViewModel.GamesLost),
                nameof(RankingRowViewModel.GamesWithdrawn),
                nameof(RankingRowViewModel.GoalsFor),
                nameof(RankingRowViewModel.GoalsAgainst),
                nameof(RankingRowViewModel.GoalsDifference),
                nameof(RankingRowViewModel.LastMatches)
            ]);

        public static RankingListParameterProvider Compact => new([
                nameof(RankingRowViewModel.Rank),
                nameof(RankingRowViewModel.Team),
                nameof(RankingRowViewModel.Points),
                nameof(RankingRowViewModel.Played),
                nameof(RankingRowViewModel.GoalsDifference)
    ]);

        private RankingListParameterProvider(string[] defaultColumns) => _display = new DisplayViewModel().AddMode(new DisplayModeList(defaultColumns), true);

        private readonly SortingViewModel _sort = new(nameof(RankingRowViewModel.Rank));

        public override ISortingViewModel ProvideSorting() => _sort;

        public override IDisplayViewModel ProvideDisplay() => _display;
    }
}
