// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Media;
using DynamicData.Binding;
using MyNet.UI.Commands;
using MyNet.Utilities;
using MyNet.Utilities.Messaging;
using MyNet.Observable;
using MyClub.Teamup.Application.Messages;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Wpf.Extensions;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class MatchViewModel : EntityViewModelBase<Match>, IAppointment
    {
        private readonly MatchPresentationService _matchPresentationService;
        private readonly StadiumsProvider _stadiumsProvider;

        public MatchViewModel(Match item, IMatchParent parent, StadiumsProvider stadiumsProvider, MatchPresentationService matchPresentationService) : base(item)
        {
            _matchPresentationService = matchPresentationService;
            _stadiumsProvider = stadiumsProvider;

            Parent = parent;
            HomeTeam = parent.GetAvailableTeams().First(x => x.Id == item.HomeTeam.Id);
            AwayTeam = parent.GetAvailableTeams().First(x => x.Id == item.AwayTeam.Id);

            EditCommand = CommandsManager.Create(async () => await EditAsync().ConfigureAwait(false));
            RemoveCommand = CommandsManager.Create(async () => await RemoveAsync().ConfigureAwait(false));
            OpenCommand = CommandsManager.Create(Open);
            OpenParentCommand = CommandsManager.Create(OpenParent);
            StartCommand = CommandsManager.Create(async () => await StartAsync().ConfigureAwait(false), () => State is MatchState.None or MatchState.Suspended);
            SuspendCommand = CommandsManager.Create(async () => await SuspendAsync().ConfigureAwait(false), () => State is MatchState.InProgress);
            PostponeCommand = CommandsManager.Create(async () => await PostponeAsync().ConfigureAwait(false), () => CanUpdateScore);
            CancelCommand = CommandsManager.Create(async () => await CancelAsync().ConfigureAwait(false), () => State is MatchState.None && parent.CanCancelMatch());
            ResetCommand = CommandsManager.Create(async () => await ResetAsync().ConfigureAwait(false));
            DoWithdrawForHomeTeamCommand = CommandsManager.Create(async () => await DoWithdrawForHomeTeamAsync().ConfigureAwait(false), () => CanUpdateScore);
            DoWithdrawForAwayTeamCommand = CommandsManager.Create(async () => await DoWithdrawForAwayTeamAsync().ConfigureAwait(false), () => CanUpdateScore);
            InvertTeamsCommand = CommandsManager.Create(async () => await InvertTeamsAsync().ConfigureAwait(false), () => State is MatchState.None);

            var scoreChangedRequestedSubject = new Subject<bool>();
            Disposables.AddRange(
                [
                scoreChangedRequestedSubject.Throttle(20.Milliseconds()).Subscribe(_ =>
                {
                    RaiseScoreProperties();
                    ScoreChanged?.Invoke(this, EventArgs.Empty);
                }),
                item.Home.Events.ToObservableChangeSet()
                                .Merge(item.Away.Events.ToObservableChangeSet())
                                .Subscribe(_ => scoreChangedRequestedSubject.OnNext(true)),
                item.Home.WhenPropertyChanged(x => x.IsWithdrawn, false)
                         .Merge(item.Away.WhenPropertyChanged(x => x.IsWithdrawn, false))
                         .Subscribe(_ => scoreChangedRequestedSubject.OnNext(true)),
                this.WhenPropertyChanged(x => x.AfterExtraTime, false).Subscribe(_ => scoreChangedRequestedSubject.OnNext(true)),
                this.WhenPropertyChanged(x => x.State, false).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(IsPlayed));
                    scoreChangedRequestedSubject.OnNext(true);
                }),
                this.WhenPropertyChanged(x => x.HomeTeam, false).Subscribe(_ => HomeTeam = parent.GetAvailableTeams().First(x => x.Id == item.HomeTeam.Id)),
                this.WhenPropertyChanged(x => x.AwayTeam, false).Subscribe(_ => AwayTeam = parent.GetAvailableTeams().First(x => x.Id == item.AwayTeam.Id)),
                this.WhenPropertyChanged(x => x.Date, false).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(StartDate));
                    RaisePropertyChanged(nameof(EndDate));
                })
            ]);

            if (parent.GetCompetition() is LeagueViewModel league)
            {
                Disposables.Add(league.SubscribeOnRankingRefreshed(() =>
                {
                    HomeRank = league.Ranking.GetRow(HomeTeam)?.Rank ?? 0;
                    AwayRank = league.Ranking.GetRow(AwayTeam)?.Rank ?? 0;
                }));
            }

            Messenger.Default.Register<StadiumsChangedMessage>(this, _ => RaisePropertyChanged(nameof(Stadium)));
        }

        public Color Color => Parent.Color;

        public bool CanUpdateScore => State is MatchState.None or MatchState.InProgress or MatchState.Suspended;

        public event EventHandler? ScoreChanged;

        public IMatchParent Parent { get; }

        public TeamViewModel HomeTeam { get; private set; }

        public TeamViewModel AwayTeam { get; private set; }

        public bool NeutralVenue => Item.NeutralVenue;

        public StadiumViewModel? Stadium => Item.Stadium is not null ? _stadiumsProvider.Get(Item.Stadium.Id) : null;

        public int HomeScore => Item.Home.GetScore();

        public int AwayScore => Item.Away.GetScore();

        public int HomeShootoutScore => Item.Home.GetShootoutScore();

        public int AwayShootoutScore => Item.Away.GetShootoutScore();

        public bool HomeIsWithdrawn => Item.Home.IsWithdrawn;

        public bool AwayIsWithdrawn => Item.Away.IsWithdrawn;

        public bool AfterExtraTime => Item.Format.ExtraTimeIsEnabled && Item.AfterExtraTime;

        public bool AfterShootouts => Item.Format.ShootoutIsEnabled && Item.IsDraw() && (HomeShootoutScore > 0 || AwayShootoutScore > 0);

        public bool HomeHasWon => Item.IsWonBy(Item.HomeTeam);

        public bool AwayHasWon => Item.IsWonBy(Item.AwayTeam);

        public bool HomeHasWonAfterExtraTime => AfterExtraTime && Item.IsWonBy(Item.HomeTeam);

        public bool AwayHasWonAfterExtraTime => AfterExtraTime && Item.IsWonBy(Item.AwayTeam);

        public bool HomeHasWonAfterShootouts => AfterShootouts && Item.IsWonBy(Item.HomeTeam);

        public bool AwayHasWonAfterShootouts => AfterShootouts && Item.IsWonBy(Item.AwayTeam);

        public DateTime Date => Item.Date.ToLocalTime();

        public DateTime StartDate => Item.Date.ToLocalTime();

        public DateTime EndDate => Item.Date.AddFluentTimeSpan(GetTotalTime()).ToLocalTime();

        public MatchState State => Item.State;

        public bool IsMyMatch => HomeTeam.IsMyTeam || AwayTeam.IsMyTeam;

        public bool IsPlayed => State is MatchState.Played;

        public int HomeRank { get; private set; }

        public int AwayRank { get; private set; }

        public ICommand OpenCommand { get; }

        public ICommand OpenParentCommand { get; }

        public ICommand EditCommand { get; }

        public ICommand ResetCommand { get; }

        public ICommand StartCommand { get; }

        public ICommand PostponeCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand SuspendCommand { get; }

        public ICommand DoWithdrawForHomeTeamCommand { get; }

        public ICommand DoWithdrawForAwayTeamCommand { get; }

        public ICommand InvertTeamsCommand { get; }

        public ICommand RemoveCommand { get; }

        public bool Participate(TeamViewModel team) => Item.Participate(team.Id);

        public MatchResult GetResult(TeamViewModel team) => Item.GetResultOf(team.Id);

        public TimeSpan GetTotalTime() => Item.Format.RegulationTime.Duration * Item.Format.RegulationTime.Number + 15.Minutes();

        public void Open() => NavigationCommandsService.NavigateToMatchPage(this);

        public void OpenParent() => NavigationCommandsService.NavigateToMatchParent(Parent);

        public async Task EditAsync() => await _matchPresentationService.EditAsync(this).ConfigureAwait(false);

        public async Task StartAsync() => await _matchPresentationService.StartAsync(this).ConfigureAwait(false);

        public async Task ResetAsync() => await _matchPresentationService.ResetAsync(this).ConfigureAwait(false);

        public async Task CancelAsync() => await _matchPresentationService.CancelAsync(this).ConfigureAwait(false);

        public async Task PostponeAsync() => await _matchPresentationService.PostponeAsync(this).ConfigureAwait(false);

        public async Task SuspendAsync() => await _matchPresentationService.SuspendAsync(this).ConfigureAwait(false);

        public async Task DoWithdrawForHomeTeamAsync() => await _matchPresentationService.DoWithdrawForHomeTeamAsync(this).ConfigureAwait(false);

        public async Task DoWithdrawForAwayTeamAsync() => await _matchPresentationService.DoWithdrawForAwayTeamAsync(this).ConfigureAwait(false);

        public async Task InvertTeamsAsync() => await _matchPresentationService.InvertTeamsAsync(this).ConfigureAwait(false);

        public async Task RemoveAsync() => await _matchPresentationService.RemoveAsync([this]).ConfigureAwait(false);

        private void RaiseScoreProperties()
        {
            RaisePropertyChanged(nameof(HomeScore));
            RaisePropertyChanged(nameof(AwayScore));
            RaisePropertyChanged(nameof(HomeShootoutScore));
            RaisePropertyChanged(nameof(AwayShootoutScore));
            RaisePropertyChanged(nameof(HomeIsWithdrawn));
            RaisePropertyChanged(nameof(AwayIsWithdrawn));
            RaisePropertyChanged(nameof(AfterExtraTime));
            RaisePropertyChanged(nameof(AfterShootouts));

            RaisePropertyChanged(nameof(HomeHasWon));
            RaisePropertyChanged(nameof(AwayHasWon));
            RaisePropertyChanged(nameof(HomeHasWonAfterExtraTime));
            RaisePropertyChanged(nameof(AwayHasWonAfterExtraTime));
            RaisePropertyChanged(nameof(HomeHasWonAfterShootouts));
            RaisePropertyChanged(nameof(AwayHasWonAfterShootouts));

            RaisePropertyChanged(nameof(CanUpdateScore));
        }
    }
}
