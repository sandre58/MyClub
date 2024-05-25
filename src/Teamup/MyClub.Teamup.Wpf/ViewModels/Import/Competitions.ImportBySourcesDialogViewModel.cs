// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Teamup.Wpf.Services.Providers;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.Import;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class CompetitionsImportBySourcesDialogViewModel : ImportBySourcesDialogViewModel<CompetitionImportableViewModel, CompetitionsImportListViewModel>
    {
        public CompetitionsImportBySourcesDialogViewModel(ProjectInfoProvider projectInfoProvider, CompetitionsImportBySourcesProvider provider)
            : base(provider, new CompetitionsImportListViewModel(provider))
            => projectInfoProvider.WhenProjectChanging(_ => Reset());
    }

    internal class CompetitionsImportListViewModel : ImportListViewModel<CompetitionImportableViewModel>
    {
        public CompetitionsImportListViewModel(CompetitionsImportBySourcesProvider provider)
            : base(provider, new CompetitionImportablesListParametersProvider())
        {
            ClearStartDateCommand = CommandsManager.Create(() => ApplyOnSelection(y => y.StartDate = null));
            ClearEndDateCommand = CommandsManager.Create(() => ApplyOnSelection(y => y.EndDate = null));
            ClearLogoCommand = CommandsManager.Create(() => ApplyOnSelection(y => y.Logo = null));
        }

        public ICommand ClearStartDateCommand { get; }

        public ICommand ClearEndDateCommand { get; }

        public ICommand ClearLogoCommand { get; }
    }
}
