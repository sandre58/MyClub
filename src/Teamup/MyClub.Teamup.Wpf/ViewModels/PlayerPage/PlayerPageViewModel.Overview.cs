// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.Commands;
using MyNet.UI.Dialogs;
using MyNet.UI.ViewModels.Workspace;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.Misc;

namespace MyClub.Teamup.Wpf.ViewModels.PlayerPage
{
    internal class PlayerPageOverviewViewModel : SubItemViewModel<PlayerViewModel>
    {
        public ICommand ShowGoogleMapCommand { get; }

        public PlayerPageOverviewViewModel() => ShowGoogleMapCommand = CommandsManager.Create(async () =>
        {
            var vm = new GoogleMapDialogViewModel(Item!.FullAddress);
            await DialogManager.ShowAsync(vm).ConfigureAwait(false);
        }, () => !string.IsNullOrEmpty(Item?.FullAddress));
    }
}
