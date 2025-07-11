// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Commands;

namespace MyClub.Scorer.Wpf.ViewModels.BracketPage
{
    internal class RoundsViewModel : StagesViewModel<RoundViewModel>
    {
        private readonly RoundPresentationService _roundPresentationService;

        public RoundsViewModel(IRoundsStageViewModel stage,
                       RoundPresentationService roundPresentationService,
                       CompetitionCommandsService competitionCommandsService)
        : base(new ObservableSourceProvider<RoundViewModel>(stage.Rounds.ToObservableChangeSet()),
               new RoundsListParametersProvider(),
              competitionCommandsService)
        {
            _roundPresentationService = roundPresentationService;
            Stage = stage;
            Stages = new RoundStagesViewModel(stage, roundPresentationService, competitionCommandsService);
            DetailsViewModel = new();

            ToggleCalendarCommand = CommandsManager.Create(() => ShowCalendar = !ShowCalendar);
        }

        public RoundStagesViewModel Stages { get; }

        public IRoundsStageViewModel Stage { get; }

        public RoundDetailsViewModel DetailsViewModel { get; private set; }

        public bool ShowCalendar { get; set; }

        public ICommand ToggleCalendarCommand { get; private set; }

        protected override async Task AddItemAsync(DateTime? date = null) => await _roundPresentationService.AddAsync(Stage, date).ConfigureAwait(false);

        protected override async Task RemoveItemsAsync(IEnumerable<RoundViewModel> oldItems) => await _roundPresentationService.RemoveAsync(oldItems).ConfigureAwait(false);

        protected override void OnSelectionChanged()
        {
            base.OnSelectionChanged();

            DetailsViewModel.SetItem(SelectedItem);
        }
    }
}
