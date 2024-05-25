// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Plugins.Contracts;
using MyClub.Teamup.Plugins.Contracts.Dtos;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class PlayersImportSourceViewModel : ImportSourceViewModel<PlayerImportDto, PlayerImportableViewModel>
    {
        public PlayersImportSourceViewModel(IImportPlayersSourcePlugin source, PlayerService playerService)
            : base(source, new PlayerImportableConverter(playerService)) { }
    }
}
