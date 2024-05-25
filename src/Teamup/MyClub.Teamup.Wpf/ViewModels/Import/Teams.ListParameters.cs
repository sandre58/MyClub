// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.ViewModels.List;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class TeamImportablesListParametersProvider() : ListParametersProvider(nameof(TeamImportableViewModel.Name))
    {
        public override IFiltersViewModel ProvideFilters() => new TeamImportablesSpeedFiltersViewModel();
    }

    internal class TeamImportablesSpeedFiltersViewModel : SpeedFiltersViewModel
    {
        public TeamImportablesSpeedFiltersViewModel() => AddRange([NameFilter, CountryFilter]);

        public StringFilterViewModel NameFilter { get; } = new(nameof(TeamImportableViewModel.Name));

        public CountryFilterViewModel CountryFilter { get; } = new(nameof(TeamImportableViewModel.Country));
    }
}
