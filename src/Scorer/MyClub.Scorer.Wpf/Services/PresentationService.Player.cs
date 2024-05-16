// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MyNet.UI.Dialogs;
using MyNet.UI.Locators;
using MyNet.UI.Extensions;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyClub.Scorer.Wpf.ViewModels.Entities;

namespace MyClub.Scorer.Wpf.Services
{
    internal class PlayerPresentationService(IViewModelLocator viewModelLocator)
    {
        private readonly IViewModelLocator _viewModelLocator = viewModelLocator;

        public async Task OpenAsync(PlayerViewModel item) => await EditAsync(item).ConfigureAwait(false);

        public async Task EditAsync(PlayerViewModel item)
        {
            var vm = _viewModelLocator.Get<PlayerEditionViewModel>();
            vm.Load(item.Team, item.Id);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }
    }
}
