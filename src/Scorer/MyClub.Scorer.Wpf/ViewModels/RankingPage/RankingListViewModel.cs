// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DynamicData.Binding;
using MyClub.Scorer.Domain.RankingAggregate;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using MyNet.Utilities.Sequences;

namespace MyClub.Scorer.Wpf.ViewModels.RankingPage
{
    internal class RankingListViewModel : ListViewModel<RankingRowViewModel>
    {
        public RankingListViewModel(RankingViewModel ranking, ListParametersProvider? listParametersProvider = null)
            : base(ranking.ToObservableChangeSet<RankingViewModel, RankingRowViewModel>(),
                   parametersProvider: listParametersProvider ?? new RankingListParameterProvider())
        {
            NavigateToPastPositionsCommand = CommandsManager.CreateNotNull<TeamViewModel>(x => NavigationCommandsService.NavigateToPastPositionsPage([x.Id]));

            Disposables.AddRange(
                [
                    ranking.WhenPropertyChanged(x => Rules).Subscribe(_ => Rules = ranking.Rules),
                    ranking.WhenPropertyChanged(x => PenaltyPoints).Subscribe(_ => PenaltyPoints = ranking.PenaltyPoints),
                    ranking.WhenPropertyChanged(x => Labels).Subscribe(_ => Labels = ranking.Labels)
                ]);

            Display.Mode?.CastIn<DisplayModeList>().Reset();
        }

        public ICommand NavigateToPastPositionsCommand { get; private set; }

        public RankingRules? Rules { get; private set; }

        public ReadOnlyDictionary<TeamViewModel, int>? PenaltyPoints { get; private set; }

        public ReadOnlyDictionary<AcceptableValueRange<int>, RankLabel>? Labels { get; private set; }
    }
}
