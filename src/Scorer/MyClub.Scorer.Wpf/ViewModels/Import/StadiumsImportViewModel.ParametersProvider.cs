// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.ViewModels.List;
using MyNet.UI.ViewModels.List.Filtering;

namespace MyClub.Scorer.Wpf.ViewModels.Import
{
    internal class StadiumsImportListParametersProvider : ListParametersProvider
    {
        public StadiumsImportListParametersProvider() : base(nameof(StadiumImportableViewModel.City)) { }

        public override IFiltersViewModel ProvideFilters() => new StadiumsImportSpeedFiltersViewModel();
    }
}
