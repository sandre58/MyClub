﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Wpf.Services.Deferrers;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.UI.Dialogs;
using MyNet.UI.Extensions;
using MyNet.UI.Locators;
using MyNet.UI.Services;

namespace MyClub.Scorer.Wpf.Services
{
    internal class MatchdayPresentationService(MatchdayService service,
                                               ResultsChangedDeferrer resultsChangedDeferrer,
                                               ScheduleChangedDeferrer scheduleChangedDeferrer,
                                               IViewModelLocator viewModelLocator)
        : PresentationServiceBase<MatchdayViewModel, MatchdayEditionViewModel, MatchdayService>(service, viewModelLocator)
    {
        private readonly ResultsChangedDeferrer _resultsChangedDeferrer = resultsChangedDeferrer;
        private readonly ScheduleChangedDeferrer _scheduleChangedDeferrer = scheduleChangedDeferrer;

        public async Task OpenAsync(MatchdayViewModel item) => await EditAsync(item).ConfigureAwait(false);

        public async Task AddMultipleAsync(IMatchdayParent parent)
        {
            var vm = ViewModelLocator.Get<MatchdaysEditionViewModel>();
            vm.Load(parent);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public async Task AddAsync(IMatchdayParent parent, DateTime? date = null)
        {
            var vm = ViewModelLocator.Get<MatchdayEditionViewModel>();
            vm.New(parent, () =>
            {
                if (date.HasValue)
                {
                    vm.Date = date.Value.Date;

                    if (date.Value.TimeOfDay != TimeSpan.Zero)
                        vm.Time = date.Value.TimeOfDay;
                }
            });

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public override async Task EditAsync(MatchdayViewModel matchday)
        {
            var vm = ViewModelLocator.Get<MatchdayEditionViewModel>();
            vm.Load(matchday);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        protected override void Remove(ICollection<Guid> idsList)
        {
            using (_resultsChangedDeferrer.Defer())
                base.Remove(idsList);
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

            await AppBusyManager.WaitAsync(() =>
            {
                using (_scheduleChangedDeferrer.Defer())
                    Service.Postpone(idsList, postponedDate);
            }).ConfigureAwait(false);
        }
    }
}
