// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyNet.Observable.Collections.Providers;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.UI.Commands;

namespace MyClub.Scorer.Wpf.ViewModels.BracketPage
{
    internal class MatchdaysViewModel : MatchParentsViewModel<MatchdayViewModel>
    {
        private readonly MatchdayPresentationService _matchdayPresentationService;

        public MatchdaysViewModel(ISourceProvider<MatchdayViewModel> matchdaysProvider,
                                  MatchdayPresentationService matchdayPresentationService)
            : base(matchdaysProvider, new MatchdaysListParametersProvider())
        {
            _matchdayPresentationService = matchdayPresentationService;

            AddMultipleCommand = CommandsManager.Create(async () => await AddMultipleAsync().ConfigureAwait(false));
        }

        public ICommand AddMultipleCommand { get; }

        private async Task AddMultipleAsync() => await _matchdayPresentationService.AddMultipleAsync().ConfigureAwait(false);

        protected override async Task AddToDateAsync(DateTime date) => await _matchdayPresentationService.AddAsync(date: date).ConfigureAwait(false);

        protected override async Task AddItemAsync() => await _matchdayPresentationService.AddAsync().ConfigureAwait(false);

        protected override async Task EditItemAsync(MatchdayViewModel oldItem) => await _matchdayPresentationService.EditAsync(oldItem).ConfigureAwait(false);

        protected override async Task RemoveItemsAsync(IEnumerable<MatchdayViewModel> oldItems) => await _matchdayPresentationService.RemoveAsync(oldItems).ConfigureAwait(false);

        protected override async Task DuplicateAsync(MatchdayViewModel item) => await _matchdayPresentationService.DuplicateAsync(item).ConfigureAwait(false);

        protected override async Task PostponeAsync(IEnumerable<MatchdayViewModel> oldItems) => await _matchdayPresentationService.PostponeAsync(oldItems).ConfigureAwait(false);
    }
}
