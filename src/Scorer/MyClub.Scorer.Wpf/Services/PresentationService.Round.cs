// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyClub.Scorer.Wpf.ViewModels.SchedulePage;
using MyNet.UI.Dialogs;
using MyNet.UI.Extensions;
using MyNet.UI.Locators;
using MyNet.UI.Services;

namespace MyClub.Scorer.Wpf.Services
{
    internal class RoundPresentationService(RoundService service,
                                            IViewModelLocator viewModelLocator)
        : PresentationServiceBase<RoundViewModel, RoundEditionViewModel, RoundService>(service, viewModelLocator)
    {
        public Task OpenAsync(RoundViewModel item)
        {
            NavigationCommandsService.NavigateToSchedulePage(nameof(DisplayModeByStage), item.Id);

            return Task.CompletedTask;
        }

        public async Task AddAsync(IRoundsStageViewModel stage, DateTime? date = null)
        {
            var vm = ViewModelLocator.Get<RoundEditionViewModel>();
            vm.New(stage, date);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public override async Task EditAsync(RoundViewModel round)
        {
            var vm = ViewModelLocator.Get<RoundEditionViewModel>();
            vm.Load(round);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public async Task EditAsync(RoundStageViewModel roundStage)
        {
            var vm = ViewModelLocator.Get<RoundEditionViewModel>();
            vm.Load(roundStage);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public async Task PostponeAsync(RoundStageViewModel item, DateTime? postponedDate = null) => await PostponeAsync([item], postponedDate).ConfigureAwait(false);

        public async Task PostponeAsync(IEnumerable<RoundStageViewModel> items, DateTime? postponedDate = null)
        {
            var roundStages = items.ToList();

            if (roundStages.Count == 0) return;

            await AppBusyManager.WaitAsync(() => Service.Postpone(roundStages.Select(x => x.Id).ToList(), postponedDate)).ConfigureAwait(false);
        }
    }
}
