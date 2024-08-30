// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.UI.ViewModels.List;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyNet.UI.ViewModels.List.Sorting;

namespace MyClub.Scorer.Wpf.ViewModels.TeamsPage
{
    internal class TeamsListParametersProvider : ListParametersProvider
    {
        public override IFiltersViewModel ProvideFilters() => new StringFilterViewModel(nameof(ITeamViewModel.Name));

        public override ISortingViewModel ProvideSorting()
            => new ExtendedSortingViewModel([nameof(TeamViewModel.Name), nameof(ITeamViewModel.Country)], [nameof(ITeamViewModel.Name)]);
    }
}
