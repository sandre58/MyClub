// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyClub.Teamup.Domain.Enums;
using MyClub.Domain.Enums;
using MyClub.Teamup.Wpf.Filters;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.CommunicationPage
{
    internal class PlayersSpeedFiltersViewModel : SpeedFiltersViewModel
    {
        public PlayersSpeedFiltersViewModel() => AddRange([NameFilter, CategoryFilter, TeamsFilter, LicenseStateFilter, IsMutationFilter, GenderFilter]);

        public StringFilterViewModel NameFilter { get; } = new(nameof(PlayerViewModel.InverseName));

        public EnumerationValuesFilterViewModel<Category> CategoryFilter { get; } = new(nameof(PlayerViewModel.Category));

        public TeamsFilterViewModel TeamsFilter { get; } = new TeamsFilterViewModel(nameof(PlayerViewModel.TeamId));

        public EnumValuesFilterViewModel<LicenseState> LicenseStateFilter { get; } = new(nameof(PlayerViewModel.LicenseState));

        public BooleanFilterViewModel IsMutationFilter { get; } = new(nameof(PlayerViewModel.IsMutation)) { AllowNullValue = true };

        public GenderFilterViewModel GenderFilter { get; } = new GenderFilterViewModel(nameof(PlayerViewModel.Gender)) { IsReadOnly = true };
    }
}
