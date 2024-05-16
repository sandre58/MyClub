// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.UI.ViewModels.List;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Sorting;

namespace MyClub.Teamup.Wpf.ViewModels.CompetitionsPage.CompetitionsTab
{
    internal class CompetitionsListParametersProvider : ListParametersProvider
    {
        public override IFiltersViewModel ProvideFilters() => new CompetitionsSpeedFiltersViewModel();


        public override ISortingViewModel ProvideSorting()
            => new ExtendedSortingViewModel(new Dictionary<string, string>
            {
                { nameof(MyClubResources.Name), nameof(CompetitionViewModel.Name) },
                { nameof(MyClubResources.Type), nameof(CompetitionViewModel.Type) },
                { nameof(MyClubResources.StartDate), nameof(CompetitionViewModel.StartDate) },
                { nameof(MyClubResources.EndDate), nameof(CompetitionViewModel.EndDate) }
            }, [nameof(CompetitionViewModel.Type)]);
    }
}
