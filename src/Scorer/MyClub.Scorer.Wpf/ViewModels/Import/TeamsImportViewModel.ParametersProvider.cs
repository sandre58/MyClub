// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.ViewModels.List;
using MyNet.UI.ViewModels.List.Filtering;

namespace MyClub.Scorer.Wpf.ViewModels.Import
{
    internal class TeamsImportListParametersProvider : ListParametersProvider
    {
        public TeamsImportListParametersProvider() : base(nameof(TeamImportableViewModel.Name)) { }

        public override IFiltersViewModel ProvideFilters() => new TeamsImportSpeedFiltersViewModel();
    }
}
