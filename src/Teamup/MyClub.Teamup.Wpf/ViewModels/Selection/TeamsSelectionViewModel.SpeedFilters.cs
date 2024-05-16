// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyClub.Teamup.Wpf.ViewModels.Import;

namespace MyClub.Teamup.Wpf.ViewModels.Selection
{
    internal class TeamsSpeedFiltersViewModel : SpeedFiltersViewModel
    {
        public TeamsSpeedFiltersViewModel() => AddRange([NameFilter, CountryFilter]);

        public StringFilterViewModel NameFilter { get; } = new(nameof(TeamImportableViewModel.Name));

        public CountryFilterViewModel CountryFilter { get; } = new(nameof(TeamImportableViewModel.Country));
    }
}
