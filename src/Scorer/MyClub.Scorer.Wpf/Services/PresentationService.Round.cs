// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyClub.Scorer.Wpf.ViewModels.SchedulePage;
using MyNet.UI.Dialogs;
using MyNet.UI.Extensions;
using MyNet.UI.Locators;

namespace MyClub.Scorer.Wpf.Services
{
    internal class RoundPresentationService(RoundService service,
                                            IViewModelLocator viewModelLocator)
        : PresentationServiceBase<IRoundViewModel, RoundEditionViewModel, RoundService>(service, viewModelLocator)
    {
        public Task OpenAsync(IRoundViewModel item)
        {
            NavigationCommandsService.NavigateToSchedulePage(nameof(DisplayModeByStage), item.Id);

            return Task.CompletedTask;
        }

        public async Task AddAsync(IRoundsStageViewModel cup)
        {
            var vm = ViewModelLocator.Get<RoundEditionViewModel>();
            vm.New(cup);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public override async Task EditAsync(IRoundViewModel round)
        {
            var vm = ViewModelLocator.Get<RoundEditionViewModel>();
            vm.Load(round);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }
    }
}
