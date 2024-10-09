// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.UI.Collections;
using MyNet.UI.Commands;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class MatchOpponentViewModel : ObservableObject
    {
        private MatchOpponent? _matchOpponent;
        private readonly MatchViewModel _match;
        private readonly MatchPresentationService _matchPresentationService;
        private readonly UiObservableCollection<GoalViewModel> _goals = [];
        private readonly UiObservableCollection<PenaltyShootoutViewModel> _shootout = [];
        private readonly UiObservableCollection<CardViewModel> _cards = [];
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
                    _matchOpponent = x.opponent;
                    IsEnabled = _matchOpponent is not null;
                    Team = x.team ?? x.virtualTeam;

                    if (_matchOpponent is null || Team is not TeamViewModel team) return;

                    _opponentDisposables =
                    [
                        _matchOpponent.Events.ToObservableChangeSet()
                                             .Filter(x => x is Goal)
                                             .Transform(x => new GoalViewModel((Goal)x, team))
                                             .Bind(_goals)
                                             .DisposeMany()
                                             .Subscribe(),
                        _matchOpponent.Events.ToObservableChangeSet()
                                             .Filter(x => x is Card)
                                             .Transform(x => new CardViewModel((Card)x, team))
                                             .Bind(_cards)
                                             .DisposeMany()
                                             .Subscribe(),
                        _matchOpponent.Shootout.ToObservableChangeSet()
                                               .Transform(x => new PenaltyShootoutViewModel(x, team))
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
        }

        public bool IsEnabled { get; private set; }

        public IVirtualTeamViewModel Team { get; private set; } = null!;

        public ReadOnlyObservableCollection<GoalViewModel> Goals { get; }

        public ReadOnlyObservableCollection<PenaltyShootoutViewModel> Shootouts { get; }

        public ReadOnlyObservableCollection<CardViewModel> Cards { get; }

        public int Score => _matchOpponent?.GetScore() ?? 0;

        public int ShootoutScore => _matchOpponent?.GetShootoutScore() ?? 0;

        public bool IsWithdrawn => _matchOpponent?.IsWithdrawn ?? false;

        public bool HasWon => _match.IsWonBy(Team);

        public bool HasWonAfterExtraTime => _match.AfterExtraTime && _match.IsWonBy(Team);

        public bool HasWonAfterShootouts => _match.AfterShootouts && _match.IsWonBy(Team);

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

        protected override void Cleanup()
        {
            _opponentDisposables?.Dispose();
            base.Cleanup();
        }
    }
}
