// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.UI.Dialogs;
using MyNet.UI.Extensions;
using MyNet.UI.Locators;

namespace MyClub.Scorer.Wpf.Services
{
    internal class PersonPresentationService(IViewModelLocator viewModelLocator)
    {
        private readonly IViewModelLocator _viewModelLocator = viewModelLocator;

        public async Task OpenAsync(PlayerViewModel item) => await EditAsync(item).ConfigureAwait(false);

        public async Task EditAsync(PlayerViewModel item)
        {
            var vm = _viewModelLocator.Get<PlayerEditionViewModel>();
            vm.Load(item.Team, item.Id);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public async Task OpenAsync(ManagerViewModel item) => await EditAsync(item).ConfigureAwait(false);

        public async Task EditAsync(ManagerViewModel item)
        {
            var vm = _viewModelLocator.Get<ManagerEditionViewModel>();
            vm.Load(item.Team, item.Id);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }
    }
}
