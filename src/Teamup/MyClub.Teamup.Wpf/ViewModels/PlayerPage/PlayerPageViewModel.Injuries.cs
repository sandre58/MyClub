// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.Workspace;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.PlayerPage
{
    internal class PlayerPageInjuriesViewModel : SubItemViewModel<PlayerViewModel>
    {
        public InjuryViewModel? SelectedInjury { get; set; }

        public ICommand AddCommand { get; }

        public ICommand EditSelectedInjuryCommand { get; }

        public ICommand RemoveSelectedInjuryCommand { get; }

        public PlayerPageInjuriesViewModel()
        {
            AddCommand = CommandsManager.Create(async () => await AddInjuryAsync().ConfigureAwait(false), () => Item is not null);
            EditSelectedInjuryCommand = CommandsManager.Create(async () => await SelectedInjury!.EditAsync().ConfigureAwait(false), () => Item is not null && SelectedInjury is not null);
            RemoveSelectedInjuryCommand = CommandsManager.Create(async () => await SelectedInjury!.RemoveAsync().ConfigureAwait(false), () => Item is not null && SelectedInjury is not null);
        }

        private async Task AddInjuryAsync() => SelectedInjury = await Item!.AddInjuryAsync().ConfigureAwait(false);
    }
}
