// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Wpf.Services;
using MyNet.Observable;
using MyNet.UI.Commands;
using MyNet.UI.Helpers;
using MyNet.UI.ViewModels.FileHistory;

namespace MyClub.Scorer.Wpf.ViewModels.HomePage.OpenAssistantContent
{
    internal class OpenAssistantViewModel : ObservableObject
    {
        public string ProductName { get; } = ApplicationHelper.GetProductName();

        public RecentFilesViewModel RecentFilesViewModel { get; }

        public ICommand LoadCommand { get; }

        public ICommand CreateCommand { get; }

        public ICommand NewLeagueCommand { get; }

        public ICommand NewCupCommand { get; }

        public OpenAssistantViewModel(RecentFilesViewModel recentFilesViewModel, ProjectCommandsService projectCommandsService)
        {
            RecentFilesViewModel = recentFilesViewModel;

            LoadCommand = CommandsManager.Create(async () => await projectCommandsService.LoadAsync().ConfigureAwait(false), projectCommandsService.IsEnabled);
            CreateCommand = CommandsManager.Create(async () => await projectCommandsService.CreateAsync().ConfigureAwait(false), projectCommandsService.IsEnabled);
            NewLeagueCommand = CommandsManager.Create(async () => await projectCommandsService.NewAsync(CompetitionType.League).ConfigureAwait(false), projectCommandsService.IsEnabled);
            NewCupCommand = CommandsManager.Create(async () => await projectCommandsService.NewAsync(CompetitionType.Cup).ConfigureAwait(false), projectCommandsService.IsEnabled);
        }
    }
}
