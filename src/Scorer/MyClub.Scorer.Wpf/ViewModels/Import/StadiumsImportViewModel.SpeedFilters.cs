// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyClub.Domain.Enums;

namespace MyClub.Scorer.Wpf.ViewModels.Import
{
    internal class StadiumsImportSpeedFiltersViewModel : SpeedFiltersViewModel
    {
        public StadiumsImportSpeedFiltersViewModel() => AddRange([NameFilter, CityFilter, GroundFilter]);

        public StringFilterViewModel NameFilter { get; } = new(nameof(StadiumImportableViewModel.Name));

        public StringFilterViewModel CityFilter { get; } = new(nameof(StadiumImportableViewModel.City));

        public EnumValuesFilterViewModel<Ground> GroundFilter { get; } = new(nameof(StadiumImportableViewModel.Ground));
    }
}
