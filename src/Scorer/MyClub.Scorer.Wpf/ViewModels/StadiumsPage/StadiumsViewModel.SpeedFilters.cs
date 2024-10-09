// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Domain.Enums;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Wpf.ViewModels.StadiumsPage
{
    internal class StadiumsSpeedFiltersViewModel : SpeedFiltersViewModel
    {
        public StadiumsSpeedFiltersViewModel() => AddRange([NameFilter, CityFilter, GroundFilter]);

        public StringFilterViewModel NameFilter { get; } = new(nameof(StadiumViewModel.Name));

        public StringFilterViewModel CityFilter { get; } = new($"{nameof(StadiumViewModel.Address)}.{nameof(Address.City)}");

        public EnumValuesFilterViewModel<Ground> GroundFilter { get; } = new(nameof(StadiumViewModel.Ground));
    }
}
