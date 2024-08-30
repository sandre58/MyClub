// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyNet.UI.Dialogs;
using MyNet.UI.Dialogs.Models;
using MyNet.UI.Locators;
using MyNet.UI.Extensions;
using MyNet.UI.Services;
using MyNet.Utilities;
using MyNet.Humanizer;
using MyNet.UI.Resources;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Wpf.ViewModels.Edition;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;

namespace MyClub.Teamup.Wpf.Services
{
    internal class RoundPresentationService(RoundService service, MatchdayService matchdayService, IViewModelLocator viewModelLocator)
        : PresentationServiceBase<IMatchdayViewModel, MatchdayEditionViewModel, RoundService>(service, viewModelLocator)
    {
        private readonly MatchdayService _matchdayService = matchdayService;

        public override async Task EditAsync(IMatchdayViewModel item)
        {
            IDialogViewModel vm = null!;

            switch (item)
            {
                case MatchdayViewModel matchday:
                    var matchdayVm = ViewModelLocator.Get<MatchdayEditionViewModel>();
                    matchdayVm.Load(matchday.Parent, matchday.Id);
                    vm = matchdayVm;
                    break;
                case KnockoutViewModel knockout:
                    var knockoutVm = ViewModelLocator.Get<KnockoutEditionViewModel>();
                    knockoutVm.Load(knockout.Parent, knockout.Id);
                    vm = knockoutVm;
                    break;
            }

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public async Task EditAsync(RoundViewModel item)
        {
            IDialogViewModel vm = null!;

            switch (item)
            {
                case GroupStageViewModel groupStage:
                    var groupStageVm = ViewModelLocator.Get<GroupStageEditionViewModel>();
                    groupStageVm.Load(groupStage.Parent, groupStage.Id);
                    vm = groupStageVm;
                    break;
                case KnockoutViewModel knockout:
                    var knockoutVm = ViewModelLocator.Get<KnockoutEditionViewModel>();
                    knockoutVm.Load(knockout.Parent, knockout.Id);
                    vm = knockoutVm;
                    break;
            }

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public async Task<KnockoutViewModel?> AddKnockoutAsync(CupViewModel parent, DateTime? date = null)
        {
            var vm = ViewModelLocator.Get<KnockoutEditionViewModel>();
            vm.New(parent, () => date.IfNotNull(x => vm.Date = x));

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            return result.HasValue && result.Value ? parent.Rounds.OfType<KnockoutViewModel>().FirstOrDefault(x => x.Id == vm.ItemId) : null;
        }

        public async Task<GroupStageViewModel?> AddGroupStageAsync(CupViewModel parent, DateTime? startDate = null, DateTime? endDate = null)
        {
            var vm = ViewModelLocator.Get<GroupStageEditionViewModel>();
            vm.New(parent, () =>
            {
                startDate.IfNotNull(x => vm.StartDate = x);
                endDate.IfNotNull(x => vm.EndDate = x);
            });

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            return result.HasValue && result.Value ? parent.Rounds.OfType<GroupStageViewModel>().FirstOrDefault(x => x.Id == vm.ItemId) : null;
        }

        public async Task PostponeAsync(IMatchdayViewModel item, DateTime? postponedDate = null) => await PostponeAsync([item], postponedDate).ConfigureAwait(false);

        public async Task PostponeAsync(IEnumerable<IMatchdayViewModel> items, DateTime? postponedDate = null)
        {
            var idsListOfRounds = items.OfType<RoundViewModel>().Select(x => x.Id).ToList();
            var idsListOfMatchdays = items.OfType<MatchdayViewModel>().Select(x => x.Id).ToList();

            if (idsListOfRounds.Count + idsListOfMatchdays.Count == 0) return;

            await AppBusyManager.WaitAsync(() =>
            {
                Service.Postpone(idsListOfRounds, postponedDate);
                _matchdayService.Postpone(idsListOfMatchdays, postponedDate);
            }).ConfigureAwait(false);
        }

        public override async Task RemoveAsync(IEnumerable<IMatchdayViewModel> oldItems)
        {
            var idsListOfRounds = oldItems.OfType<RoundViewModel>().Select(x => x.Id).ToList();
            var idsListOfMatchdays = oldItems.OfType<MatchdayViewModel>().Select(x => x.Id).ToList();

            if (idsListOfRounds.Count + idsListOfMatchdays.Count == 0) return;

            var cancel = await DialogManager.ShowQuestionAsync(nameof(MessageResources.XItemsRemovingQuestion).TranslateAndFormatWithCount(idsListOfRounds.Count + idsListOfMatchdays.Count)!, UiResources.Removing).ConfigureAwait(false) != MessageBoxResult.Yes;

            if (!cancel)
            {
                await AppBusyManager.WaitAsync(() =>
                {
                    _ = Service.Remove(idsListOfRounds);
                    _ = _matchdayService.Remove(idsListOfMatchdays);
                });
            }
        }

        public async Task RemoveAsync(IEnumerable<RoundViewModel> oldItems)
        {
            var idsList = oldItems.Select(x => x.Id).ToList();

            if (idsList.Count == 0) return;

            var cancel = await DialogManager.ShowQuestionAsync(nameof(MessageResources.XItemsRemovingQuestion).TranslateAndFormatWithCount(idsList.Count)!, UiResources.Removing).ConfigureAwait(false) != MessageBoxResult.Yes;

            if (!cancel)
            {
                await AppBusyManager.WaitAsync(() => _ = Service.Remove(idsList));
            }
        }
    }
}
