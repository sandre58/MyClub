// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using DynamicData.Binding;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable.Collections.Providers;

namespace MyClub.Scorer.Wpf.ViewModels.BracketPage
{
    internal class RoundsViewModel : BracketItemsViewModel<IRoundViewModel>
    {
        private readonly RoundPresentationService _roundPresentationService;

        public RoundsViewModel(CupViewModel stage,
                               RoundPresentationService roundPresentationService)
            : base(new ObservableSourceProvider<IRoundViewModel>(stage.Rounds.ToObservableChangeSet()), new MatchdaysListParametersProvider())
        {
            _roundPresentationService = roundPresentationService;
            Stage = stage;
        }

        public CupViewModel Stage { get; }

        protected override async Task AddItemAsync() => await _roundPresentationService.AddAsync(Stage).ConfigureAwait(false);

        protected override async Task EditItemAsync(IRoundViewModel oldItem) => await _roundPresentationService.EditAsync(oldItem).ConfigureAwait(false);

        protected override async Task RemoveItemsAsync(IEnumerable<IRoundViewModel> oldItems) => await _roundPresentationService.RemoveAsync(oldItems).ConfigureAwait(false);
    }
}
