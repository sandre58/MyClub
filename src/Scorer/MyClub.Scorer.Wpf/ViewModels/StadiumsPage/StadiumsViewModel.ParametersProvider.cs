// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.UI.ViewModels.List;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Sorting;
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Wpf.ViewModels.StadiumsPage
{
    internal class StadiumsListParametersProvider : ListParametersProvider
    {
        public override IFiltersViewModel ProvideFilters() => new StadiumsSpeedFiltersViewModel();

        public override ISortingViewModel ProvideSorting()
            => new ExtendedSortingViewModel(new Dictionary<string, string>
            {
                { nameof(MyClubResources.Name), nameof(StadiumViewModel.Name) },
                { nameof(MyClubResources.City), $"{nameof(StadiumViewModel.Address)}.{nameof(Address.City)}" },
                { nameof(MyClubResources.Ground), nameof(StadiumViewModel.Ground) },
            }, [$"{nameof(StadiumViewModel.Address)}.{nameof(Address.City)}"]);
    }
}
