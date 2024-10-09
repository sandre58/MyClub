// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Wpf.Services.Providers;
using MyNet.UI.ViewModels.Import;

namespace MyClub.Scorer.Wpf.ViewModels.Import
{
    internal class TeamsImportBySourcesDialogViewModel : ImportBySourcesDialogViewModel<TeamImportableViewModel, TeamsImportListViewModel>
    {
        private readonly ProjectInfoProvider? _projectInfoProvider;

        public TeamsImportBySourcesDialogViewModel(ProjectInfoProvider projectInfoProvider, TeamsImportBySourcesProvider provider)
            : base(provider, new TeamsImportListViewModel(provider))
        {
            _projectInfoProvider = projectInfoProvider;
            projectInfoProvider.UnloadRunner.RegisterOnStart(this, Reset);
        }

        public TeamsImportBySourcesDialogViewModel(TeamsImportBySourcesProvider provider) : base(provider, new TeamsImportListViewModel(provider)) { }

        protected override void Cleanup()
        {
            _projectInfoProvider?.UnloadRunner.Unregister(this);
            base.Cleanup();
        }
    }

    internal class TeamsImportListViewModel : ImportListViewModel<TeamImportableViewModel>
    {
        public TeamsImportListViewModel(TeamsImportBySourcesProvider provider)
            : base(provider, new TeamImportablesListParametersProvider()) { }
    }
}
