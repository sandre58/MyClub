// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Wpf.Messages;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyClub.Scorer.Wpf.ViewModels.SchedulingAssistant;
using MyNet.Humanizer;
using MyNet.UI.Dialogs;
using MyNet.UI.Extensions;
using MyNet.UI.Locators;
using MyNet.UI.Resources;
using MyNet.UI.Services;
using MyNet.Utilities;
using MyNet.Utilities.Messaging;
using MyNet.Utilities.Units;

namespace MyClub.Scorer.Wpf.Services
{
    internal class MatchPresentationService(MatchService matchService,
                                            IViewModelLocator viewModelLocator)
    {
        private readonly MatchService _matchService = matchService;
        private readonly IViewModelLocator _viewModelLocator = viewModelLocator;

        public async Task OpenAsync(MatchViewModel item) => await EditAsync(item).ConfigureAwait(false);

        public async Task AddAsync(IMatchParent? parent = null)
        {
            var vm = _viewModelLocator.Get<MatchEditionViewModel>();
            vm.New(parent);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public async Task EditAsync(MatchViewModel item)
        {
            var vm = _viewModelLocator.Get<MatchEditionViewModel>();

            vm.Load(item);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public async Task RemoveAsync(IEnumerable<MatchViewModel> oldItems)
        {
            var idsList = oldItems.Select(x => x.Id).ToList();

            if (idsList.Count == 0) return;

            var cancel = await DialogManager.ShowQuestionAsync(nameof(MessageResources.XItemsRemovingQuestion).TranslateWithCountAndOptionalFormat(idsList.Count)!, UiResources.Removing).ConfigureAwait(false) != MessageBoxResult.Yes;

            if (!cancel)
            {
                await AppBusyManager.WaitAsync(() =>
                {
                    _matchService.Remove(idsList);
                    CheckConflicts();
                });
            }
        }

        public async Task SaveScoresAsync(IEnumerable<MatchDto> items)
        {
            var idsList = items.Select(x => x.Id).ToList();

            if (idsList.Count == 0) return;

            await AppBusyManager.WaitAsync(() => _matchService.SaveScores(items)).ConfigureAwait(false);
        }

        public async Task StartAsync(MatchViewModel item) => await StartAsync([item]).ConfigureAwait(false);

        public async Task StartAsync(IEnumerable<MatchViewModel> items)
        {
            var idsList = items.Select(x => x.Id).ToList();

            if (idsList.Count == 0) return;

            await AppBusyManager.WaitAsync(() => _matchService.Start(idsList)).ConfigureAwait(false);
        }

        public async Task RescheduleAsync(MatchViewModel item, int offset, TimeUnit timeUnit) => await RescheduleAsync([item], offset, timeUnit).ConfigureAwait(false);

        public async Task RescheduleAsync(IEnumerable<MatchViewModel> items, int offset, TimeUnit timeUnit)
        {
            var idsList = items.Select(x => x.Id).ToList();

            if (idsList.Count == 0) return;

            await AppBusyManager.WaitAsync(() =>
            {
                _matchService.Reschedule(idsList, offset, timeUnit);
                CheckConflicts();
            }).ConfigureAwait(false);
        }

        public async Task RescheduleAsync(IEnumerable<MatchViewModel> items, DateTime date)
        {
            var idsList = items.Select(x => x.Id).ToList();

            if (idsList.Count == 0) return;

            await AppBusyManager.WaitAsync(() =>
            {
                _matchService.Reschedule(idsList, date);
                CheckConflicts();
            }).ConfigureAwait(false);
        }

        public async Task PostponeAsync(MatchViewModel item, DateTime? postponedDate = null) => await PostponeAsync([item], postponedDate).ConfigureAwait(false);

        public async Task PostponeAsync(IEnumerable<MatchViewModel> items, DateTime? postponedDate = null)
        {
            var idsList = items.Select(x => x.Id).ToList();

            if (idsList.Count == 0) return;

            await AppBusyManager.WaitAsync(() =>
            {
                _matchService.Postpone(idsList, postponedDate);
                CheckConflicts();
            }).ConfigureAwait(false);
        }

        public async Task CancelAsync(MatchViewModel item) => await CancelAsync([item]).ConfigureAwait(false);

        public async Task CancelAsync(IEnumerable<MatchViewModel> items)
        {
            var idsList = items.Select(x => x.Id).ToList();

            if (idsList.Count == 0) return;

            await AppBusyManager.WaitAsync(() =>
            {
                _matchService.Cancel(idsList);
                CheckConflicts();
            }).ConfigureAwait(false);
        }

        public async Task SuspendAsync(MatchViewModel item) => await SuspendAsync([item]).ConfigureAwait(false);

        public async Task SuspendAsync(IEnumerable<MatchViewModel> items)
        {
            var idsList = items.Select(x => x.Id).ToList();

            if (idsList.Count == 0) return;

            await AppBusyManager.WaitAsync(() => _matchService.Suspend(idsList)).ConfigureAwait(false);
        }

        public async Task ResetAsync(MatchViewModel item) => await ResetAsync([item]).ConfigureAwait(false);

        public async Task ResetAsync(IEnumerable<MatchViewModel> items)
        {
            var idsList = items.Select(x => x.Id).ToList();

            if (idsList.Count == 0) return;

            await AppBusyManager.WaitAsync(() => _matchService.Reset(idsList)).ConfigureAwait(false);
        }

        public async Task FinishAsync(MatchViewModel item) => await FinishAsync([item]).ConfigureAwait(false);

        public async Task FinishAsync(IEnumerable<MatchViewModel> items)
        {
            var idsList = items.Select(x => x.Id).ToList();

            if (idsList.Count == 0) return;

            await AppBusyManager.WaitAsync(() => _matchService.Finish(idsList)).ConfigureAwait(false);
        }

        public async Task DoWithdrawForHomeTeamAsync(MatchViewModel item) => await AppBusyManager.WaitAsync(() => _matchService.DoWithdraw(item.Id, item.HomeTeam.Id)).ConfigureAwait(false);

        public async Task DoWithdrawForHomeTeamAsync(IEnumerable<MatchViewModel> items) => await AppBusyManager.WaitAsync(() => _matchService.DoWithdraw(items.Select(x => (x.Id, x.HomeTeam.Id)))).ConfigureAwait(false);

        public async Task DoWithdrawForAwayTeamAsync(MatchViewModel item) => await AppBusyManager.WaitAsync(() => _matchService.DoWithdraw(item.Id, item.AwayTeam.Id)).ConfigureAwait(false);

        public async Task DoWithdrawForAwayTeamAsync(IEnumerable<MatchViewModel> items) => await AppBusyManager.WaitAsync(() => _matchService.DoWithdraw(items.Select(x => (x.Id, x.AwayTeam.Id)))).ConfigureAwait(false);

        public async Task RandomizeAsync(MatchViewModel item) => await RandomizeAsync([item]).ConfigureAwait(false);

        public async Task RandomizeAsync(IEnumerable<MatchViewModel> items)
        {
            var idsList = items.Select(x => x.Id).ToList();

            if (idsList.Count == 0) return;

            await AppBusyManager.WaitAsync(() => _matchService.Randomize(idsList)).ConfigureAwait(false);
        }

        public async Task InvertTeamsAsync(MatchViewModel item) => await AppBusyManager.WaitAsync(() => _matchService.InvertTeams(item.Id)).ConfigureAwait(false);

        public async Task InvertTeamsAsync(IEnumerable<MatchViewModel> items)
        {
            var idsList = items.Select(x => x.Id).ToList();

            if (idsList.Count == 0) return;

            await AppBusyManager.WaitAsync(() => _matchService.InvertTeams(idsList)).ConfigureAwait(false);
        }

        public async Task SetStadiumAsync(IEnumerable<MatchViewModel> items, StadiumViewModel? stadium)
        {
            var idsList = items.Select(x => x.Id).ToList();

            if (idsList.Count == 0) return;

            await AppBusyManager.WaitAsync(() =>
            {
                _matchService.SetStadium(idsList, stadium?.Id);
                CheckConflicts();
            }).ConfigureAwait(false);
        }

        public async Task OpenSchedulingAssistantAsync(IEnumerable<MatchViewModel> matches, DateTime? displayDate = null)
        {
            var vm = _viewModelLocator.Get<SchedulingAssistantViewModel>();
            vm.Load(matches, displayDate);

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            if (result.IsFalse()) return;

            var dtos = vm.Matches.Wrappers.Where(x => x.IsModified()).Select(x => new MatchDto
            {
                Id = x.Item.Id,
                Date = x.StartDate,
                Stadium = x.Stadium is not null
                          ? new StadiumDto
                          {
                              Id = x.Stadium.Id
                          } : null
            }).ToList();

            await AppBusyManager.WaitAsync(() =>
            {
                _matchService.Reschedule(dtos);
                CheckConflicts();
            }).ConfigureAwait(false);
        }

        private static void CheckConflicts() => Messenger.Default.Send(new CheckConflictsRequestMessage());
    }
}
