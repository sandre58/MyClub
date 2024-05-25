// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Domain.Enums;
using MyNet.UI.ViewModels.List;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;

namespace MyClub.Scorer.Wpf.ViewModels.Import
{
    internal class StadiumImportablesListParametersProvider() : ListParametersProvider(nameof(StadiumImportableViewModel.City))
    {
        public override IFiltersViewModel ProvideFilters() => new StadiumImportablesSpeedFiltersViewModel();
    }

    internal class StadiumImportablesSpeedFiltersViewModel : SpeedFiltersViewModel
    {
        public StadiumImportablesSpeedFiltersViewModel() => AddRange([NameFilter, CityFilter, GroundFilter]);

        public StringFilterViewModel NameFilter { get; } = new(nameof(StadiumImportableViewModel.Name));

        public StringFilterViewModel CityFilter { get; } = new(nameof(StadiumImportableViewModel.City));

        public EnumValuesFilterViewModel<Ground> GroundFilter { get; } = new(nameof(StadiumImportableViewModel.Ground));
    }
}
