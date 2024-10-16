// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.DynamicData.Extensions;
using MyNet.Observable;
using MyNet.UI.Commands;
using MyNet.UI.Threading;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class MatchOpponentViewModel : ObservableObject
    {
        private MatchOpponent? _matchOpponent;
        private readonly MatchViewModel _match;
        private readonly MatchPresentationService _matchPresentationService;
        private readonly ExtendedObservableCollection<GoalViewModel> _goals = [];
        private readonly ExtendedObservableCollection<PenaltyShootoutViewModel> _shootout = [];
        private readonly ExtendedObservableCollection<CardViewModel> _cards = [];
        private CompositeDisposable? _opponentDisposables;

        public MatchOpponentViewModel(IObservable<(MatchOpponent? opponent, TeamViewModel? team, IVirtualTeamViewModel virtualTeam)> observable,
                                      MatchViewModel match,
                                      MatchPresentationService matchPresentationService)
        {
            _matchPresentationService = matchPresentationService;

            _match = match;
            Goals = new(_goals);
            Shootouts = new(_shootout);
            Cards = new(_cards);

            AddGoalCommand = CommandsManager.Create(async () => await AddGoalAsync().ConfigureAwait(false), () => IsEnabled);
            RemoveGoalCommand = CommandsManager.Create(async () => await RemoveGoalAsync().ConfigureAwait(false), () => IsEnabled && Score > 0);
            AddSucceededPenaltyShootoutCommand = CommandsManager.Create(async () => await AddSucceededPenaltyShootoutAsync().ConfigureAwait(false), () => IsEnabled);
            RemoveSucceededPenaltyShootoutCommand = CommandsManager.Create(async () => await RemoveSucceededPenaltyShootoutAsync().ConfigureAwait(false), () => IsEnabled && ShootoutScore > 0);
            DoWithdrawCommand = CommandsManager.Create(async () => await DoWithdrawAsync().ConfigureAwait(false), () => IsEnabled && _match.CanDoWithdraw());

            Disposables.AddRange(
            [
                observable.Subscribe(x =>
                {
                    if (Equals(_matchOpponent, x)) return;

                    _opponentDisposables?.Dispose();

                    MyNet.UI.Threading.Scheduler.GetUIOrCurrent().Schedule(() => {
                    _cards.Clear();
                    _goals.Clear();
                    _shootout.Clear();
                    });
                    _matchOpponent = x.opponent;
                    IsEnabled = _matchOpponent is not null;
                    Team = x.team ?? x.virtualTeam;

                    if (_matchOpponent is null || Team is not TeamViewModel team) return;

                    _opponentDisposables =
                    [
                        _matchOpponent.Events.ToObservableChangeSet()
                                             .Filter(x => x is Goal)
                                             .Transform(x => new GoalViewModel((Goal)x, team))
                                             .ObserveOn(MyNet.UI.Threading.Scheduler.GetUIOrCurrent())
                                             .Bind(_goals)
                                             .DisposeMany()
                                             .Subscribe(),
                        _matchOpponent.Events.ToObservableChangeSet()
                                             .Filter(x => x is Card)
                                             .Transform(x => new CardViewModel((Card)x, team))
                                             .ObserveOn(MyNet.UI.Threading.Scheduler.GetUIOrCurrent())
                                             .Bind(_cards)
                                             .DisposeMany()
                                             .Subscribe(),
                        _matchOpponent.Shootout.ToObservableChangeSet()
                                               .Transform(x => new PenaltyShootoutViewModel(x, team))
                                               .ObserveOn(MyNet.UI.Threading.Scheduler.GetUIOrCurrent())
                                               .Bind(_shootout)
                                               .DisposeMany()
                                               .Subscribe(),
                        _matchOpponent.WhenPropertyChanged(x => x.IsWithdrawn, false).Subscribe(_ => RaisePropertyChanged(nameof(IsWithdrawn))),
                    ];
                }),
                _goals.ToObservableChangeSet().Subscribe(_ => RaisePropertyChanged(nameof(Score))),
                _shootout.ToObservableChangeSet().Subscribe(_ => RaisePropertyChanged(nameof(ShootoutScore))),
                Observable.FromEventPattern(x => _match.ScoreChanged += x, x => _match.ScoreChanged -= x).Subscribe(x => RaiseScoreProperties())
            ]);

            if (match.Fixture is not null)
            {
                ComputeFixtureState();
                Disposables.Add(match.Fixture.Matches.ToObservableChangeSet().SubscribeMany(x => Observable.FromEventPattern<EventHandler, EventArgs>(y => x.ScoreChanged += y, y => x.ScoreChanged -= y).Subscribe(_ => ComputeFixtureState())).Subscribe());
            }
        }

        public bool IsEnabled { get; private set; }

        public IVirtualTeamViewModel Team { get; private set; } = null!;

        public ReadOnlyObservableCollection<GoalViewModel> Goals { get; }

        public ReadOnlyObservableCollection<PenaltyShootoutViewModel> Shootouts { get; }

        public ReadOnlyObservableCollection<CardViewModel> Cards { get; }

        public int Score => _matchOpponent?.GetScore() ?? 0;

        public int ShootoutScore => _matchOpponent?.GetShootoutScore() ?? 0;

        public bool IsWithdrawn => _matchOpponent?.IsWithdrawn ?? false;

        public bool HasWon => Team is TeamViewModel team && _match.IsWonBy(team);

        public bool HasWonAfterExtraTime => _match.AfterExtraTime && Team is TeamViewModel team && _match.IsWonBy(team);

        public bool HasWonAfterShootouts => _match.AfterShootouts && Team is TeamViewModel team && _match.IsWonBy(team);

        public QualificationState QualificationState { get; private set; }

        public ICommand AddGoalCommand { get; }

        public ICommand RemoveGoalCommand { get; }

        public ICommand AddSucceededPenaltyShootoutCommand { get; }

        public ICommand RemoveSucceededPenaltyShootoutCommand { get; }

        public ICommand DoWithdrawCommand { get; }

        public async Task DoWithdrawAsync()
        {
            if (Team is TeamViewModel team)
                await _matchPresentationService.DoWithdrawAsync(_match, team).ConfigureAwait(false);
        }

        public async Task AddGoalAsync()
        {
            if (Team is TeamViewModel team)
                await _matchPresentationService.AddGoalAsync(_match, team).ConfigureAwait(false);
        }

        public async Task RemoveGoalAsync()
        {
            if (Team is TeamViewModel team)
                await _matchPresentationService.RemoveGoalAsync(_match, team).ConfigureAwait(false);
        }

        public async Task AddSucceededPenaltyShootoutAsync()
        {
            if (Team is TeamViewModel team)
                await _matchPresentationService.AddSucceededPenaltyShootoutAsync(_match, team).ConfigureAwait(false);
        }

        public async Task RemoveSucceededPenaltyShootoutAsync()
        {
            if (Team is TeamViewModel team)
                await _matchPresentationService.RemoveSucceededPenaltyShootoutAsync(_match, team).ConfigureAwait(false);
        }

        private void RaiseScoreProperties()
        {
            RaisePropertyChanged(nameof(HasWon));
            RaisePropertyChanged(nameof(HasWonAfterExtraTime));
            RaisePropertyChanged(nameof(HasWonAfterShootouts));
        }

        private void ComputeFixtureState()
        {
            if (_match.Fixture is null || Team is not TeamViewModel team)
            {
                QualificationState = QualificationState.Unknown;
                return;
            }
            var isQualified = _match.Fixture.IsWonBy(team);
            var isEliminated = _match.Fixture.IsLostBy(team);
            var isTemporary = !_match.Fixture.IsPlayed();

            QualificationState = isQualified && !isTemporary
                ? QualificationState.IsQualified
                : isEliminated && !isTemporary
                ? QualificationState.IsEliminated
                : isQualified && isTemporary
                ? QualificationState.IsTemporaryQualified
                : isEliminated && isTemporary ? QualificationState.IsTemporaryEliminated : QualificationState.Unknown;
        }

        protected override void Cleanup()
        {
            _opponentDisposables?.Dispose();
            base.Cleanup();
        }
    }
}
