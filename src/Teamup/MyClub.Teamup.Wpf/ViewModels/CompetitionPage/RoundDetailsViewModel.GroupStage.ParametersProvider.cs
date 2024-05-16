// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.CompetitionPage
{
    internal class GroupStageMatchesListParametersProvider(GroupStageViewModel groupStage) : ListParametersProvider(nameof(MatchViewModel.Date))
    {
        private readonly GroupStageViewModel _groupStage = groupStage;

        public override IFiltersViewModel ProvideFilters() => new GroupStageMatchesSpeedFiltersViewModel(_groupStage);
    }
}
