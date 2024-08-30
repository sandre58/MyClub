// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
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
                { nameof(MyClubResources.Name), nameof(IStadiumViewModel.Name) },
                { nameof(MyClubResources.City), $"{nameof(IStadiumViewModel.Address)}.{nameof(Address.City)}" },
                { nameof(MyClubResources.Ground), nameof(IStadiumViewModel.Ground) },
            }, [$"{nameof(IStadiumViewModel.Address)}.{nameof(Address.City)}"]);
    }
}
