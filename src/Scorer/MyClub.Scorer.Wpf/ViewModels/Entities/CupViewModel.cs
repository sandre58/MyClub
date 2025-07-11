// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.DynamicData.Extensions;
using MyNet.UI.Threading;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class CupViewModel : EntityViewModelBase<Cup>, ICompetitionViewModel, IRoundsStageViewModel
    {
        private readonly ExtendedObservableCollection<RoundViewModel> _rounds = [];
        private readonly ExtendedObservableCollection<MatchViewModel> _matches = [];

        public CupViewModel(Cup item,
                            IObservable<SchedulingParameters?> observableSchedulingParameters,
                            RoundPresentationService roundPresentationService,
                            MatchPresentationService matchPresentationService,
                            StadiumsProvider stadiumsProvider,
                            TeamsProvider teamsProvider) : base(item)
        {
            Rounds = new(_rounds);
            Matches = new(_matches);
            SchedulingParameters = new SchedulingParametersViewModel(observableSchedulingParameters);

            Disposables.AddRange(
            [
                item.Rounds.ToObservableChangeSet()
                           .Transform(x => new RoundViewModel(x, this, observableSchedulingParameters, roundPresentationService, matchPresentationService, teamsProvider, stadiumsProvider))
                           .ObserveOn(Scheduler.GetUIOrCurrent())
                           .Bind(_rounds)
                           .DisposeMany()
                           .Subscribe(),
                _rounds.ToObservableChangeSet()
                       .MergeManyEx(x => x.Matches.ToObservableChangeSet())
                       .ObserveOn(Scheduler.GetUIOrCurrent())
                       .Bind(_matches)
                       .Subscribe(),

                _rounds.ToObservableChangeSet().Subscribe(_ => UpdateDisplayNames())
            ]);
        }

        public SchedulingParametersViewModel SchedulingParameters { get; private set; }

        public ReadOnlyObservableCollection<RoundViewModel> Rounds { get; }

        public ReadOnlyObservableCollection<MatchViewModel> Matches { get; }

        public string Name => string.Empty;

        public string ShortName => string.Empty;

        private void UpdateDisplayNames() => _rounds.SelectMany(x => x.Fixtures).ForEach((x, y) =>
        {
            x.DisplayName = (y + 1).ToString(MyClubResources.FixtureX);
            x.DisplayShortName = (y + 1).ToString(MyClubResources.FixtureXAbbr);
        });

        protected override void OnCultureChanged()
        {
            UpdateDisplayNames();
            base.OnCultureChanged();
        }
    }
}
