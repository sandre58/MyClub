// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Wpf.Services.Providers;
using MyNet.UI.ViewModels.Import;

namespace MyClub.Scorer.Wpf.ViewModels.Import
{
    internal class StadiumsImportBySourcesDialogViewModel : ImportBySourcesDialogViewModel<StadiumImportableViewModel, StadiumsImportListViewModel>
    {
        public StadiumsImportBySourcesDialogViewModel(ProjectInfoProvider projectInfoProvider, StadiumsImportBySourcesProvider provider)
            : base(provider, new StadiumsImportListViewModel(provider))
            => projectInfoProvider.WhenProjectClosing(Reset);

        public StadiumsImportBySourcesDialogViewModel(StadiumsImportBySourcesProvider provider) : base(provider, new StadiumsImportListViewModel(provider)) { }
    }

    internal class StadiumsImportListViewModel : ImportListViewModel<StadiumImportableViewModel>
    {
        public StadiumsImportListViewModel(StadiumsImportBySourcesProvider provider)
            : base(provider, new StadiumImportablesListParametersProvider()) { }
    }
}
