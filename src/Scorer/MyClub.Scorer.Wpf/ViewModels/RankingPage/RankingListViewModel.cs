// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Domain.RankingAggregate;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using MyNet.Utilities.Sequences;


namespace MyClub.Scorer.Wpf.ViewModels.RankingPage
{
    internal class RankingListViewModel : ListViewModel<RankingRowViewModel>
    {
        private readonly RankingViewModel _ranking;

        public RankingListViewModel(RankingViewModel ranking, ListParametersProvider? listParametersProvider = null)
            : base(ranking.ToObservableChangeSet<RankingViewModel, RankingRowViewModel>(),
                   parametersProvider: listParametersProvider ?? RankingListParameterProvider.Full)
        {
            _ranking = ranking;

            NavigateToPastPositionsCommand = CommandsManager.CreateNotNull<TeamViewModel>(x => NavigationCommandsService.NavigateToPastPositionsPage([x.Id]));

            ranking.UpdateRunner.Register(this, Collection.DeferRefresh);

            Disposables.AddRange(
                [
                    ranking.WhenPropertyChanged(x => Rules).Subscribe(_ => Rules = ranking.Rules),
                    ranking.WhenPropertyChanged(x => PenaltyPoints).Subscribe(_ => PenaltyPoints = ranking.PenaltyPoints),
                    ranking.WhenPropertyChanged(x => Labels).Subscribe(_ => Labels = ranking.Labels)
                ]);

            MyNet.UI.Threading.Scheduler.GetUIOrCurrent().Schedule(() => Display.Mode?.CastIn<DisplayModeList>().Reset());
        }

        public ICommand NavigateToPastPositionsCommand { get; private set; }

        public RankingRules? Rules { get; private set; }

        public ReadOnlyDictionary<TeamViewModel, int>? PenaltyPoints { get; private set; }

        public ReadOnlyDictionary<AcceptableValueRange<int>, RankLabel>? Labels { get; private set; }

        protected override void Cleanup()
        {
            _ranking.UpdateRunner.Unregister(this);
            base.Cleanup();
        }
    }
}
