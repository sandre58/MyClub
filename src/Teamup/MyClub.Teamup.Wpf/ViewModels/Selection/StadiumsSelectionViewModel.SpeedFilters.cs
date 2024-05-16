// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyClub.Teamup.Wpf.ViewModels.Import;

namespace MyClub.Teamup.Wpf.ViewModels.Selection
{
    internal class StadiumsSpeedFiltersViewModel : SpeedFiltersViewModel
    {
        public StadiumsSpeedFiltersViewModel() => AddRange([NameFilter, CityFilter]);

        public StringFilterViewModel NameFilter { get; } = new(nameof(StadiumImportableViewModel.Name));

        public StringFilterViewModel CityFilter { get; } = new(nameof(StadiumImportableViewModel.City));
    }
}
