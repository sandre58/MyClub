// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class RoundOfMatchesViewModel : RoundViewModel<RoundOfMatches>, IMatchParentViewModel
    {
        private readonly MatchPresentationService _matchPresentationService;

        public RoundOfMatchesViewModel(RoundOfMatches item,
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
            _matchPresentationService = matchPresentationService;

            Disposables.AddRange(
            [
                Matches.ToObservableChangeSet()
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

        IStageViewModel IMatchParentViewModel.Stage => Stage;

        protected override IObservable<IChangeSet<MatchViewModel>> ConnectMatches(MatchPresentationService matchPresentationService, StadiumsProvider stadiumsProvider, TeamsProvider teamsProvider)
            => Item.Matches.ToObservableChangeSet().Transform(x => new MatchViewModel(x, this, matchPresentationService, stadiumsProvider, teamsProvider)).DisposeMany();

        public async Task AddMatchAsync() => await _matchPresentationService.AddAsync(this).ConfigureAwait(false);
        public bool CanCancelMatch() => throw new NotImplementedException();
    }
}
