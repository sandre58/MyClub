// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
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
    internal class RoundOfFixturesViewModel : RoundViewModel<RoundOfFixtures>
    {
        private readonly ExtendedObservableCollection<RoundStageViewModel> _stages = [];
        private readonly ExtendedObservableCollection<FixtureViewModel> _fixtures = [];

        public RoundOfFixturesViewModel(RoundOfFixtures item,
                                        IRoundsStageViewModel stage,
                                        IObservable<SchedulingParameters?> observableSchedulingParameters,
                                        RoundPresentationService roundPresentationService,
                                        MatchPresentationService matchPresentationService,
                                        StadiumsProvider stadiumsProvider,
                                        TeamsProvider teamsProvider)
            : base(item,
                   stage,
                   observableSchedulingParameters,
                   roundPresentationService,
                   matchPresentationService,
                   teamsProvider,
                   stadiumsProvider)
        {
            Stages = new(_stages);
            Fixtures = new(_fixtures);

            Disposables.AddRange(
            [
                item.Fixtures.ToObservableChangeSet()
                             .Transform(x => new FixtureViewModel(x, this, teamsProvider))
                             .ObserveOn(Scheduler.GetUIOrCurrent())
                             .Bind(_fixtures)
                             .DisposeMany()
                             .Subscribe(),
                item.Stages.ToObservableChangeSet()
                           .Transform(x => new RoundStageViewModel(x, this, matchPresentationService, stadiumsProvider, teamsProvider))
                           .ObserveOn(Scheduler.GetUIOrCurrent())
                           .Bind(_stages)
                           .DisposeMany()
                           .Subscribe(),
                _fixtures.ToObservableChangeSet()
                        .OnItemAdded(x =>
                        {
                           teamsProvider.RegisterVirtualTeam(x.WinnerTeam);
                           teamsProvider.RegisterVirtualTeam(x.LooserTeam);
                        })
                        .OnItemRemoved(x =>
                        {
                           teamsProvider.RemoveVirtualTeam(x.WinnerTeam);
                           teamsProvider.RemoveVirtualTeam(x.LooserTeam);
                        })
                        .Subscribe(),
            ]);
        }

        public ReadOnlyObservableCollection<RoundStageViewModel> Stages { get; }

        public ReadOnlyObservableCollection<FixtureViewModel> Fixtures { get; }

        protected override IObservable<IChangeSet<MatchViewModel>> ConnectMatches(MatchPresentationService matchPresentationService, StadiumsProvider stadiumsProvider, TeamsProvider teamsProvider)
            => _stages.ToObservableChangeSet().MergeManyEx(x => x.Matches.ToObservableChangeSet());
    }
}
