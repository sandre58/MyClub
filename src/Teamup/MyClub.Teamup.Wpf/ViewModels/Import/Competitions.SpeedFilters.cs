// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class CompetitionsImportSpeedFiltersViewModel : SpeedFiltersViewModel
    {
        public CompetitionsImportSpeedFiltersViewModel() => AddRange([NameFilter, TypeFilter]);

        public StringFilterViewModel NameFilter { get; } = new(nameof(CompetitionImportableViewModel.Name));

        public EnumValuesFilterViewModel<CompetitionType> TypeFilter { get; } = new(nameof(CompetitionImportableViewModel.Type)) { IsReadOnly = true };
    }
}
