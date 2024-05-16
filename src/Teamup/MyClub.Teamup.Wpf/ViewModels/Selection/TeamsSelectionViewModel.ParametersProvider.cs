// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Teamup.Wpf.ViewModels.Import;
using MyNet.UI.ViewModels.List;
using MyNet.UI.ViewModels.List.Filtering;

namespace MyClub.Teamup.Wpf.ViewModels.Selection
{
    internal class TeamsSelectionListParametersProvider() : ListParametersProvider(nameof(TeamImportableViewModel.Name))
    {
        public override IFiltersViewModel ProvideFilters() => new TeamsSpeedFiltersViewModel();
    }
}
