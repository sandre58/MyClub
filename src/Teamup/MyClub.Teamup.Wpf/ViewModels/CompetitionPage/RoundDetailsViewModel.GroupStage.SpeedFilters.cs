// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyNet.Utilities.Comparaison;
using MyClub.Domain.Enums;
using MyClub.Teamup.Wpf.Filters;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.CompetitionPage
{
    internal sealed class GroupStageMatchesSpeedFiltersViewModel : SpeedFiltersViewModel
    {
        public GroupStageMatchesSpeedFiltersViewModel(GroupStageViewModel groupStage)
        {
            AddRange([TeamFilter, DateFilter, StateFilter]);
            TeamFilter.Initialize(groupStage.Teams);
        }

        public MatchTeamFilterViewModel TeamFilter { get; } = new MatchTeamFilterViewModel();

        public DateFilterViewModel DateFilter { get; } = new(nameof(MatchViewModel.Date), ComplexComparableOperator.IsBetween, null, null);

        public EnumValuesFilterViewModel<MatchState> StateFilter { get; } = new(nameof(MatchViewModel.State));
    }
}
