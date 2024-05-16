// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyClub.Teamup.Wpf.Filters;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.CompetitionsPage.CompetitionsTab
{
    internal class CompetitionsSpeedFiltersViewModel : SpeedFiltersViewModel
    {
        public CompetitionsSpeedFiltersViewModel() => AddRange([NameFilter, IsCurrentFilter, TeamsFilter, TypeFilter]);
        public StringFilterViewModel NameFilter { get; } = new(nameof(CompetitionViewModel.Name));

        public BooleanFilterViewModel IsCurrentFilter { get; } = new(nameof(CompetitionViewModel.IsCurrent)) { AllowNullValue = true };

        public TeamsFilterViewModel TeamsFilter { get; } = new TeamsFilterViewModel($"{nameof(CompetitionViewModel.Teams)}.{nameof(TeamViewModel.Id)}", nameof(TeamViewModel.Id)) { ShowExternalPlayers = false, ShowLogicalOperator = false };

        public EnumValuesFilterViewModel<CompetitionType> TypeFilter { get; } = new(nameof(CompetitionViewModel.Type));
    }
}
