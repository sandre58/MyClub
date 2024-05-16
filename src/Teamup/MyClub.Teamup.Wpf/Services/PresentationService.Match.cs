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
using MyNet.Humanizer;
using MyNet.UI.Resources;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Edition;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;

namespace MyClub.Teamup.Wpf.Services
{
    internal class MatchPresentationService(MatchService matchService,
                                            StadiumsProvider stadiumsProvider,
                                            StadiumPresentationService stadiumPresentationService,
                                            TeamPresentationService teamPresentationService,
                                            IViewModelLocator viewModelLocator)
    {
        private readonly MatchService _matchService = matchService;
        private readonly StadiumsProvider _stadiumsProvider = stadiumsProvider;
        private readonly StadiumPresentationService _stadiumPresentationService = stadiumPresentationService;
        private readonly TeamPresentationService _teamPresentationService = teamPresentationService;
        private readonly IViewModelLocator _viewModelLocator = viewModelLocator;

        public async Task AddMultipleAsync(IMatchParent parent)
        {
            var vm = new MatchesAddViewModel(_matchService, _stadiumsProvider, _stadiumPresentationService, _teamPresentationService, parent);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public async Task AddAsync(IMatchParent parent, TeamViewModel? homeTeam = null, TeamViewModel? awayTeam = null)
        {
            var vm = _viewModelLocator.Get<MatchEditionViewModel>();
            vm.New(parent, () =>
            {
                vm.HomeTeam = homeTeam;
                vm.AwayTeam = awayTeam;
            });

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public async Task EditAsync(MatchViewModel item)
        {
            var vm = _viewModelLocator.Get<MatchEditionViewModel>();

            vm.Load(item.Parent, item.Id);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public async Task EditAsync(IEnumerable<MatchViewModel> items)
        {
            var vm = _viewModelLocator.Get<MatchesEditionViewModel>();
            vm.Load(items);

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public async Task RemoveAsync(IEnumerable<MatchViewModel> oldItems)
        {
            var idsList = oldItems.Select(x => x.Id).ToList();

            if (idsList.Count == 0) return;

            var cancel = await DialogManager.ShowQuestionAsync(nameof(MessageResources.XItemsRemovingQuestion).TranslateWithCountAndOptionalFormat(idsList.Count)!, UiResources.Removing).ConfigureAwait(false) != MessageBoxResult.Yes;

            if (!cancel)
            {
                await AppBusyManager.WaitAsync(() => _matchService.Remove(idsList));
            }
        }

        public async Task StartAsync(MatchViewModel item) => await StartAsync([item]).ConfigureAwait(false);

        public async Task StartAsync(IEnumerable<MatchViewModel> items)
        {
            var idsList = items.Select(x => x.Id).ToList();

            if (idsList.Count == 0) return;

            await AppBusyManager.WaitAsync(() => _matchService.Start(idsList)).ConfigureAwait(false);
        }

        public async Task PostponeAsync(MatchViewModel item, DateTime? postponedDate = null) => await PostponeAsync([item], postponedDate).ConfigureAwait(false);

        public async Task PostponeAsync(IEnumerable<MatchViewModel> items, DateTime? postponedDate = null)
        {
            var idsList = items.Select(x => x.Id).ToList();

            if (idsList.Count == 0) return;

            await AppBusyManager.WaitAsync(() => _matchService.Postpone(idsList, postponedDate)).ConfigureAwait(false);
        }

        public async Task CancelAsync(MatchViewModel item) => await CancelAsync([item]).ConfigureAwait(false);

        public async Task CancelAsync(IEnumerable<MatchViewModel> items)
        {
            var idsList = items.Select(x => x.Id).ToList();

            if (idsList.Count == 0) return;

            await AppBusyManager.WaitAsync(() => _matchService.Cancel(idsList)).ConfigureAwait(false);
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

        public async Task DoWithdrawForHomeTeamAsync(MatchViewModel item) => await AppBusyManager.WaitAsync(() => _matchService.DoWithdraw(item.Id, item.HomeTeam.Id)).ConfigureAwait(false);

        public async Task DoWithdrawForAwayTeamAsync(MatchViewModel item) => await AppBusyManager.WaitAsync(() => _matchService.DoWithdraw(item.Id, item.AwayTeam.Id)).ConfigureAwait(false);

        public async Task InvertTeamsAsync(MatchViewModel item) => await AppBusyManager.WaitAsync(() => _matchService.InvertTeams(item.Id)).ConfigureAwait(false);
    }
}
