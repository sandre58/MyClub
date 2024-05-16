// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class CompetitionsImportListParametersProvider() : ListParametersProvider(nameof(CompetitionImportableViewModel.Name))
    {
        public override IFiltersViewModel ProvideFilters() => new CompetitionsImportSpeedFiltersViewModel();
    }
}
