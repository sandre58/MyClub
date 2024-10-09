// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using DynamicData.PLinq;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.UI.Commands;
using MyNet.UI.Threading;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class RoundStageViewModel : EntityViewModelBase<RoundStage>, IMatchesStageViewModel
    {
        private readonly MatchPresentationService _matchPresentationService;
        private readonly ReadOnlyObservableCollection<MatchViewModel> _matches;

        public RoundStageViewModel(RoundStage item,
                                   RoundOfFixturesViewModel stage,
                                   MatchPresentationService matchPresentationService,
                                   StadiumsProvider stadiumsProvider,
                                   TeamsProvider teamsProvider) : base(item)
        {
            _matchPresentationService = matchPresentationService;

            Stage = stage;

            Disposables.AddRange(
            [
                item.Matches.ToObservableChangeSet()
                            .Transform(x => new MatchViewModel(x, this, _matchPresentationService, stadiumsProvider, teamsProvider))
                            .ObserveOn(Scheduler.UI)
                            .Bind(out _matches)
                            .DisposeMany()
                            .Subscribe(),
                item.WhenPropertyChanged(x => x.Date).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(StartDate));
                    RaisePropertyChanged(nameof(EndDate));
                    RaisePropertyChanged(nameof(MatchTime));
                    RaisePropertyChanged(nameof(Date));
                })
            ]);
        }

        public RoundOfFixturesViewModel Stage { get; set; }

        public SchedulingParametersViewModel SchedulingParameters => Stage.SchedulingParameters;

        public string Name => Item.Name;

        public string ShortName => Item.ShortName;

        public bool IsPostponed => Item.IsPostponed;

        public DateTime Date => Item.Date.ToCurrentTime();

        public TimeOnly MatchTime => Date.ToTime();

        public DateTime OriginDate => Item.OriginDate;

        public ReadOnlyObservableCollection<MatchViewModel> Matches => _matches;

        public DateTime StartDate => Date.BeginningOfDay();

        public DateTime EndDate => Date.EndOfDay();

        public ICommand OpenCommand { get; }

        public bool CanAutomaticReschedule() => Stage.CanAutomaticReschedule();

        public bool CanAutomaticRescheduleVenue() => Stage.CanAutomaticRescheduleVenue();

        public bool CanEditMatchFormat() => false;

        public bool CanEditMatchRules() => false;

        public bool CanCancelMatch() => Stage.CanCancelMatch();

        public bool CanBePostponed() => !Item.IsPostponed && Matches.Any(x => !x.IsPlayed);

        public IEnumerable<IVirtualTeamViewModel> GetAvailableTeams() => Stage.Teams;

        public async Task AddMatchAsync() => await _matchPresentationService.AddAsync(this).ConfigureAwait(false);
    }
}
