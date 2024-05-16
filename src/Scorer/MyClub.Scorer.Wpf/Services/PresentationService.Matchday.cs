// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyNet.UI.Dialogs;
using MyNet.UI.Locators;
using MyNet.UI.Extensions;
using MyNet.UI.Services;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Application.Services;

namespace MyClub.Scorer.Wpf.Services
{
    internal class MatchdayPresentationService(MatchdayService service, IViewModelLocator viewModelLocator)
        : PresentationServiceBase<MatchdayViewModel, MatchdayEditionViewModel, MatchdayService>(service, viewModelLocator)
    {
        public async Task OpenAsync(MatchdayViewModel item) => await EditAsync(item).ConfigureAwait(false);

        public async Task AddAsync(Guid? parentId = null, DateTime? date = null)
        {
            var vm = ViewModelLocator.Get<MatchdayEditionViewModel>();
            vm.New(parentId, () =>
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
