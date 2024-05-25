// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.UI.ViewModels.List;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class CompetitionImportablesListParametersProvider() : ListParametersProvider(nameof(CompetitionImportableViewModel.Name))
    {
        public override IFiltersViewModel ProvideFilters() => new CompetitionImportablesSpeedFiltersViewModel();
    }

    internal class CompetitionImportablesSpeedFiltersViewModel : SpeedFiltersViewModel
    {
        public CompetitionImportablesSpeedFiltersViewModel() => AddRange([NameFilter, TypeFilter]);

        public StringFilterViewModel NameFilter { get; } = new(nameof(CompetitionImportableViewModel.Name));

        public EnumValuesFilterViewModel<CompetitionType> TypeFilter { get; } = new(nameof(CompetitionImportableViewModel.Type)) { IsReadOnly = true };
    }
}
