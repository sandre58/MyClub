// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyNet.Observable.Collections.Providers;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.UI.Commands;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.UI.ViewModels.Display;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.BracketPage
{
    internal class MatchdaysViewModel : MatchParentsViewModel<MatchdayViewModel>
    {
        private readonly MatchdayPresentationService _matchdayPresentationService;

        public MatchdaysViewModel(IMatchdayParent parent,
                                  ISourceProvider<MatchdayViewModel> matchdaysProvider,
                                  MatchdayPresentationService matchdayPresentationService)
            : base(parent, matchdaysProvider, new MatchdaysListParametersProvider())
        {
            _matchdayPresentationService = matchdayPresentationService;

            AddMultipleCommand = CommandsManager.Create(async () => await AddMultipleAsync().ConfigureAwait(false));
            OpenDateCommand = CommandsManager.Create<DateTime?>(x => NavigationCommandsService.NavigateToSchedulePage(nameof(DisplayModeDay), x!.Value.ToDate()), x => x is not null);
        }

        public ICommand AddMultipleCommand { get; }

        public ICommand OpenDateCommand { get; }

        private async Task AddMultipleAsync() => await _matchdayPresentationService.AddMultipleAsync(Parent).ConfigureAwait(false);

        protected override async Task AddToDateAsync(DateTime date) => await _matchdayPresentationService.AddAsync(Parent, date).ConfigureAwait(false);

        protected override async Task AddItemAsync() => await _matchdayPresentationService.AddAsync(Parent).ConfigureAwait(false);

        protected override async Task EditItemAsync(MatchdayViewModel oldItem) => await _matchdayPresentationService.EditAsync(oldItem).ConfigureAwait(false);

        protected override async Task RemoveItemsAsync(IEnumerable<MatchdayViewModel> oldItems) => await _matchdayPresentationService.RemoveAsync(oldItems).ConfigureAwait(false);

        protected override async Task DuplicateAsync(MatchdayViewModel item) => await _matchdayPresentationService.DuplicateAsync(item).ConfigureAwait(false);

        protected override async Task PostponeAsync(IEnumerable<MatchdayViewModel> oldItems) => await _matchdayPresentationService.PostponeAsync(oldItems).ConfigureAwait(false);
    }
}
