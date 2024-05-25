// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.ViewModels.List;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class StadiumImportablesListParametersProvider() : ListParametersProvider(nameof(StadiumImportableViewModel.City))
    {
        public override IFiltersViewModel ProvideFilters() => new StadiumImportablesSpeedFiltersViewModel();
    }

    internal class StadiumImportablesSpeedFiltersViewModel : SpeedFiltersViewModel
    {
        public StadiumImportablesSpeedFiltersViewModel() => AddRange([NameFilter, CityFilter]);

        public StringFilterViewModel NameFilter { get; } = new(nameof(StadiumImportableViewModel.Name));

        public StringFilterViewModel CityFilter { get; } = new(nameof(StadiumImportableViewModel.City));
    }
}
