// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.DynamicData.Extensions;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Commands;

namespace MyClub.Scorer.Wpf.ViewModels.BracketPage
{
    internal class RoundStagesViewModel : StagesViewModel<RoundStageViewModel>
    {
        private readonly RoundPresentationService _roundPresentationService;

        public RoundStagesViewModel(IRoundsStageViewModel stage,
                                    RoundPresentationService roundPresentationService,
                                    CompetitionCommandsService competitionCommandsService)
            : base(new ObservableSourceProvider<RoundStageViewModel>(stage.Rounds.ToObservableChangeSet()
                                                                                 .MergeManyEx(x => x.Stages.ToObservableChangeSet())),
                   new RoundStagesListParametersProvider(),
                   competitionCommandsService)
        {
            _roundPresentationService = roundPresentationService;
            Stage = stage;

            PostponeSelectedItemsCommand = CommandsManager.Create(async () => await PostponeAsync(SelectedItems).ConfigureAwait(false), () => SelectedItems.All(x => x.CanBePostponed()));
        }

        public IRoundsStageViewModel Stage { get; }

        public ICommand PostponeSelectedItemsCommand { get; }

        protected override Task AddItemAsync(DateTime? date = null) => throw new NotImplementedException();

        protected override Task RemoveItemsAsync(IEnumerable<RoundStageViewModel> oldItems) => throw new NotImplementedException();

        protected async Task PostponeAsync(IEnumerable<RoundStageViewModel> oldItems) => await _roundPresentationService.PostponeAsync(oldItems).ConfigureAwait(false);
    }
}
