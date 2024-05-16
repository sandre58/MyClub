﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Linq;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.Workspace;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.ViewModels.Entities;

namespace MyClub.Scorer.Wpf.ViewModels.TeamsPage
{
    internal class TeamDetailsViewModel : ItemViewModel<TeamViewModel>
    {
        public TeamDetailsViewModel(TeamPresentationService teamPresentationService)
        {
            RemovePlayerCommand = CommandsManager.CreateNotNull<PlayerViewModel>(async x => await teamPresentationService.RemovePlayerAsync(x));
            RemoveSelectedPlayersCommand = CommandsManager.Create(async () => await teamPresentationService.RemovePlayersAsync(SelectedPlayers?.OfType<PlayerViewModel>() ?? []), () => SelectedPlayers is not null);
            AddPlayerCommand = CommandsManager.Create(async () => await teamPresentationService.AddPlayerAsync(Item!), () => Item is not null);
        }

        public ICommand RemovePlayerCommand { get; private set; }

        public ICommand RemoveSelectedPlayersCommand { get; private set; }

        public ICommand AddPlayerCommand { get; private set; }

        public IEnumerable? SelectedPlayers { get; set; }
    }
}
