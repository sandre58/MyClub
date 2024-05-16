// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Wpf.Services;
using MyNet.UI.Commands;
using MyNet.UI.Resources;
using MyNet.UI.ViewModels.FileHistory;
using MyNet.UI.ViewModels.Workspace;

namespace MyClub.Scorer.Wpf.ViewModels.Shell
{
    internal class OpenViewModel : WorkspaceViewModel
    {
        public RecentFilesViewModel RecentFilesViewModel { get; }

        public ICommand LoadCommand { get; }

        public OpenViewModel(RecentFilesViewModel recentFilesViewModel, ProjectCommandsService projectCommandsService)
        {
            RecentFilesViewModel = recentFilesViewModel;
            LoadCommand = CommandsManager.Create(async () => await projectCommandsService.LoadAsync().ConfigureAwait(false), projectCommandsService.IsEnabled);
        }

        protected override string CreateTitle() => UiResources.Open;
    }
}
