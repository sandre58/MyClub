// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Teamup.Wpf.Services;
using MyNet.Observable;
using MyNet.UI.Commands;
using MyNet.UI.Helpers;
using MyNet.UI.ViewModels.FileHistory;

namespace MyClub.Teamup.Wpf.ViewModels.HomePage.OpenAssistantContent
{
    internal class OpenAssistantViewModel : ObservableObject
    {
        public string ProductName { get; } = ApplicationHelper.GetProductName();

        public string Company { get; } = ApplicationHelper.GetCompany();

        public RecentFilesViewModel RecentFilesViewModel { get; }

        public ICommand LoadCommand { get; }

        public ICommand NewCommand { get; }

        public ICommand CreateCommand { get; }

        public OpenAssistantViewModel(RecentFilesViewModel recentFilesViewModel, ProjectCommandsService projectCommandsService)
        {
            RecentFilesViewModel = recentFilesViewModel;

            LoadCommand = CommandsManager.Create(async () => await projectCommandsService.LoadAsync().ConfigureAwait(false), projectCommandsService.IsEnabled);
            NewCommand = CommandsManager.Create(async () => await projectCommandsService.NewAsync().ConfigureAwait(false), projectCommandsService.IsEnabled);
            CreateCommand = CommandsManager.Create(async () => await projectCommandsService.CreateAsync().ConfigureAwait(false), projectCommandsService.IsEnabled);
        }
    }
}
