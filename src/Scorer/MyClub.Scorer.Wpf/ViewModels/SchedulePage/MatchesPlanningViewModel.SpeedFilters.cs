// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyNet.Utilities.Comparaison;
using MyClub.Domain.Enums;
using MyClub.Scorer.Wpf.Filters;
using MyClub.Scorer.Wpf.ViewModels.Entities;

namespace MyClub.Scorer.Wpf.ViewModels.SchedulePage
{
    internal class MatchesPlanningSpeedFiltersViewModel : SpeedFiltersViewModel
    {
        public MatchesPlanningSpeedFiltersViewModel(IEnumerable<TeamViewModel> teams, IEnumerable<StadiumViewModel> stadiums)
        {
            TeamFilter = new MatchTeamFilterViewModel(teams);
            StadiumFilter = new(nameof(MatchViewModel.Stadium), stadiums);
            AddRange([DateFilter, TeamFilter, StateFilter, StadiumFilter]);
        }

        public MyNet.UI.ViewModels.List.Filtering.Filters.DateFilterViewModel DateFilter { get; } = new(nameof(MatchViewModel.Date), ComplexComparableOperator.IsBetween, null, null);

        public MatchTeamFilterViewModel TeamFilter { get; }

        public EnumValuesFilterViewModel<MatchState> StateFilter { get; } = new(nameof(MatchViewModel.State));

        public SelectedValuesFilterViewModel<StadiumViewModel> StadiumFilter { get; }
    }
}
