// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Wpf.Services;
using MyNet.UI.Commands;
using MyNet.UI.Resources;
using MyNet.UI.ViewModels.Workspace;

namespace MyClub.Scorer.Wpf.ViewModels.Shell
{
    internal class NewViewModel : WorkspaceViewModel
    {
        public ICommand NewLeagueCommand { get; }

        public ICommand NewCupCommand { get; }

        public NewViewModel(ProjectCommandsService projectCommandsService)
        {
            NewLeagueCommand = CommandsManager.Create(async () => await projectCommandsService.NewAsync(CompetitionType.League).ConfigureAwait(false), projectCommandsService.IsEnabled);
            NewCupCommand = CommandsManager.Create(async () => await projectCommandsService.NewAsync(CompetitionType.Cup).ConfigureAwait(false), projectCommandsService.IsEnabled);
        }

        protected override string CreateTitle() => UiResources.New;
    }
}
