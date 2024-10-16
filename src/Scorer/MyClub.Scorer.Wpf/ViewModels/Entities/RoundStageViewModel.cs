// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using DynamicData.PLinq;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.DynamicData.Extensions;
using MyNet.UI.Threading;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class RoundStageViewModel : EntityViewModelBase<RoundStage>, IMatchParentViewModel
    {
        private readonly MatchPresentationService _matchPresentationService;
        private readonly ExtendedObservableCollection<MatchViewModel> _matches = [];

        public RoundStageViewModel(RoundStage item,
                                   RoundOfFixturesViewModel stage,
                                   MatchPresentationService matchPresentationService,
                                   StadiumsProvider stadiumsProvider,
                                   TeamsProvider teamsProvider) : base(item)
        {
            _matchPresentationService = matchPresentationService;

            Matches = new(_matches);
            Stage = stage;

            Disposables.AddRange(
            [
                item.Matches.ToObservableChangeSet()
                            .Transform(x => new MatchViewModel(x, this, _matchPresentationService, stadiumsProvider, teamsProvider))
                            .ObserveOn(Scheduler.GetUIOrCurrent())
                            .Bind(_matches)
                            .DisposeMany()
                            .Subscribe(),
                item.WhenPropertyChanged(x => x.Date).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(StartDate));
                    RaisePropertyChanged(nameof(EndDate));
                    RaisePropertyChanged(nameof(Date));
                })
            ]);
        }

        public RoundOfFixturesViewModel Stage { get; }

        IStageViewModel IMatchParentViewModel.Stage => Stage;

        public string Name => Item.Name;

        public string ShortName => Item.ShortName;

        public DateTime Date => Item.Date.ToCurrentTime();

        public ReadOnlyObservableCollection<MatchViewModel> Matches { get; }

        public DateTime StartDate => Date.BeginningOfDay();

        public DateTime EndDate => Date.EndOfDay();

        public bool CanAutomaticReschedule() => Stage.CanAutomaticReschedule();

        public bool CanAutomaticRescheduleVenue() => Stage.CanAutomaticRescheduleVenue();

        public bool CanCancelMatch() => true;

        public IEnumerable<IVirtualTeamViewModel> GetAvailableTeams() => Stage.Teams;

        public async Task AddMatchAsync() => await _matchPresentationService.AddAsync(this).ConfigureAwait(false);
    }
}
