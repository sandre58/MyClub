// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DynamicData.Binding;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Commands;

namespace MyClub.Scorer.Wpf.ViewModels.BracketPage
{
    internal class MatchdaysViewModel : StagesViewModel<MatchdayViewModel>
    {
        private readonly MatchdayPresentationService _matchdayPresentationService;

        public MatchdaysViewModel(IMatchdaysStageViewModel stage,
                                  MatchdayPresentationService matchdayPresentationService,
                                  CompetitionCommandsService competitionCommandsService)
            : base(new ObservableSourceProvider<MatchdayViewModel>(stage.Matchdays.ToObservableChangeSet()), new MatchdaysListParametersProvider(), competitionCommandsService)
        {
            _matchdayPresentationService = matchdayPresentationService;

            Stage = stage;
            AddMultipleCommand = CommandsManager.Create(async () => await AddMultipleAsync().ConfigureAwait(false));
            AddMatchToSelectedItemCommand = CommandsManager.Create(async () => await SelectedItem!.AddMatchAsync().ConfigureAwait(false), () => SelectedWrappers.Count == 1);
            DuplicateSelectedItemCommand = CommandsManager.Create(async () => await DuplicateAsync(SelectedItem!).ConfigureAwait(false), () => SelectedWrappers.Count == 1);
            PostponeSelectedItemsCommand = CommandsManager.Create(async () => await PostponeAsync(SelectedItems).ConfigureAwait(false), () => SelectedItems.All(x => x.CanBePostponed()));
        }

        public IMatchdaysStageViewModel Stage { get; }

        public ICommand AddMultipleCommand { get; }

        public ICommand AddMatchToSelectedItemCommand { get; }

        public ICommand DuplicateSelectedItemCommand { get; }

        public ICommand PostponeSelectedItemsCommand { get; }

        private async Task AddMultipleAsync() => await _matchdayPresentationService.AddMultipleAsync(Stage).ConfigureAwait(false);

        protected override async Task AddItemAsync(DateTime? date = null) => await _matchdayPresentationService.AddAsync(Stage, date).ConfigureAwait(false);

        protected override async Task RemoveItemsAsync(IEnumerable<MatchdayViewModel> oldItems) => await _matchdayPresentationService.RemoveAsync(oldItems).ConfigureAwait(false);

        protected async Task DuplicateAsync(MatchdayViewModel item) => await _matchdayPresentationService.DuplicateAsync(item).ConfigureAwait(false);

        protected async Task PostponeAsync(IEnumerable<MatchdayViewModel> oldItems) => await _matchdayPresentationService.PostponeAsync(oldItems).ConfigureAwait(false);
    }
}
