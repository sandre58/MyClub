﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
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
    internal class MatchdayPresentationService(MatchdayService service,
                                               IViewModelLocator viewModelLocator)
        : PresentationServiceBase<MatchdayViewModel, MatchdayEditionViewModel, MatchdayService>(service, viewModelLocator)
    {
        public Task OpenAsync(MatchdayViewModel item)
        {
            NavigationCommandsService.NavigateToSchedulePage(nameof(DisplayModeByStage), item.Id);

            return Task.CompletedTask;
        }

        public async Task AddMultipleAsync(IMatchdaysStageViewModel stage)
        {
            var vm = ViewModelLocator.Get<MatchdaysEditionViewModel>();
            vm.Load(stage);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public async Task AddAsync(IMatchdaysStageViewModel stage, DateTime? date = null)
        {
            var vm = ViewModelLocator.Get<MatchdayEditionViewModel>();
            vm.New(stage, date);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public override async Task EditAsync(MatchdayViewModel matchday)
        {
            var vm = ViewModelLocator.Get<MatchdayEditionViewModel>();
            vm.Load(matchday);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public async Task DuplicateAsync(MatchdayViewModel matchday)
        {
            var vm = ViewModelLocator.Get<MatchdayEditionViewModel>();
            vm.Duplicate(matchday);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public async Task PostponeAsync(MatchdayViewModel item, DateTime? postponedDate = null) => await PostponeAsync([item], postponedDate).ConfigureAwait(false);

        public async Task PostponeAsync(IEnumerable<MatchdayViewModel> items, DateTime? postponedDate = null)
        {
            var idsList = items.Select(x => x.Id).ToList();

            if (idsList.Count == 0) return;

            await AppBusyManager.WaitAsync(() => Service.Postpone(idsList, postponedDate)).ConfigureAwait(false);
        }
    }
}
