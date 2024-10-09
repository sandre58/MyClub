// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DynamicData.Binding;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.Display;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.BracketPage
{
    internal class MatchdaysViewModel : BracketItemsViewModel<MatchdayViewModel>
    {
        private readonly MatchdayPresentationService _matchdayPresentationService;

        public MatchdaysViewModel(LeagueViewModel stage,
                                  MatchdayPresentationService matchdayPresentationService)
            : base(new ObservableSourceProvider<MatchdayViewModel>(stage.Matchdays.ToObservableChangeSet()), new MatchdaysListParametersProvider())
        {
            _matchdayPresentationService = matchdayPresentationService;

            Stage = stage;
            AddMultipleCommand = CommandsManager.Create(async () => await AddMultipleAsync().ConfigureAwait(false));
            OpenDateCommand = CommandsManager.Create<DateTime?>(x => NavigationCommandsService.NavigateToSchedulePage(nameof(DisplayModeDay), x!.Value.ToDate()), x => x is not null);
            AddToDateCommand = CommandsManager.Create<DateTime>(async x => await AddToDateAsync(x).ConfigureAwait(false));
            AddMatchToSelectedItemCommand = CommandsManager.Create(async () => await SelectedItem!.AddMatchAsync().ConfigureAwait(false), () => SelectedWrappers.Count == 1);
            DuplicateSelectedItemCommand = CommandsManager.Create(async () => await DuplicateAsync(SelectedItem!).ConfigureAwait(false), () => SelectedWrappers.Count == 1);
            PostponeSelectedItemsCommand = CommandsManager.Create(async () => await PostponeAsync(SelectedItems).ConfigureAwait(false), () => SelectedItems.All(x => x.CanBePostponed()));
        }

        public LeagueViewModel Stage { get; }

        public ICommand AddMultipleCommand { get; }

        public ICommand OpenDateCommand { get; }

        public ICommand AddToDateCommand { get; }

        public ICommand AddMatchToSelectedItemCommand { get; }

        public ICommand DuplicateSelectedItemCommand { get; }

        public ICommand PostponeSelectedItemsCommand { get; }

        private async Task AddMultipleAsync() => await _matchdayPresentationService.AddMultipleAsync(Stage).ConfigureAwait(false);

        protected async Task AddToDateAsync(DateTime date) => await _matchdayPresentationService.AddAsync(Stage, date).ConfigureAwait(false);

        protected override async Task AddItemAsync() => await _matchdayPresentationService.AddAsync(Stage).ConfigureAwait(false);

        protected override async Task EditItemAsync(MatchdayViewModel oldItem) => await _matchdayPresentationService.EditAsync(oldItem).ConfigureAwait(false);

        protected override async Task RemoveItemsAsync(IEnumerable<MatchdayViewModel> oldItems) => await _matchdayPresentationService.RemoveAsync(oldItems).ConfigureAwait(false);

        protected async Task DuplicateAsync(MatchdayViewModel item) => await _matchdayPresentationService.DuplicateAsync(item).ConfigureAwait(false);

        protected async Task PostponeAsync(IEnumerable<MatchdayViewModel> oldItems) => await _matchdayPresentationService.PostponeAsync(oldItems).ConfigureAwait(false);
    }
}
