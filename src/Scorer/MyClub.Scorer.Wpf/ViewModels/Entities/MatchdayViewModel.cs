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
    internal class MatchdayViewModel : EntityViewModelBase<Matchday>, IMatchesStageViewModel
    {
        private readonly MatchdayPresentationService _matchdayPresentationService;
        private readonly MatchPresentationService _matchPresentationService;
        private readonly ReadOnlyObservableCollection<MatchViewModel> _matches;

        public MatchdayViewModel(Matchday item,
                                 LeagueViewModel stage,
                                 MatchdayPresentationService matchdayPresentationService,
                                 MatchPresentationService matchPresentationService,
                                 StadiumsProvider stadiumsProvider,
                                 TeamsProvider teamsProvider) : base(item)
        {
            _matchdayPresentationService = matchdayPresentationService;
            _matchPresentationService = matchPresentationService;

            Stage = stage;

            EditCommand = CommandsManager.Create(async () => await EditAsync().ConfigureAwait(false));
            DuplicateCommand = CommandsManager.Create(async () => await DuplicateAsync().ConfigureAwait(false));
            RemoveCommand = CommandsManager.Create(async () => await RemoveAsync().ConfigureAwait(false));
            OpenCommand = CommandsManager.Create(async () => await OpenAsync().ConfigureAwait(false));
            PostponeCommand = CommandsManager.Create(async () => await PostponeAsync().ConfigureAwait(false), CanBePostponed);
            AddMatchCommand = CommandsManager.Create(async () => await AddMatchAsync().ConfigureAwait(false));

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

        public LeagueViewModel Stage { get; set; }

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

        public ICommand EditCommand { get; }

        public ICommand DuplicateCommand { get; }

        public ICommand RemoveCommand { get; }

        public ICommand PostponeCommand { get; }

        public ICommand AddMatchCommand { get; }

        public bool CanAutomaticReschedule() => Stage.CanAutomaticReschedule();

        public bool CanAutomaticRescheduleVenue() => Stage.CanAutomaticRescheduleVenue();

        public bool CanEditMatchFormat() => false;

        public bool CanEditMatchRules() => false;

        public bool CanCancelMatch() => false;

        public bool CanBePostponed() => !Item.IsPostponed && Matches.Any(x => !x.IsPlayed);

        public IEnumerable<IVirtualTeamViewModel> GetAvailableTeams() => Stage.GetAvailableTeams();

        public async Task OpenAsync() => await _matchdayPresentationService.OpenAsync(this).ConfigureAwait(false);

        public async Task EditAsync() => await _matchdayPresentationService.EditAsync(this).ConfigureAwait(false);

        public async Task DuplicateAsync() => await _matchdayPresentationService.DuplicateAsync(this).ConfigureAwait(false);

        public async Task RemoveAsync() => await _matchdayPresentationService.RemoveAsync([this]).ConfigureAwait(false);

        public async Task PostponeAsync() => await _matchdayPresentationService.PostponeAsync(this).ConfigureAwait(false);

        public async Task AddMatchAsync() => await _matchPresentationService.AddAsync(this).ConfigureAwait(false);
    }
}
