﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.UI.Collections;
using MyNet.UI.Commands;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using MyNet.Utilities.Units;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class MatchViewModel : EntityViewModelBase<Match>, IAppointment
    {
        private readonly MatchPresentationService _matchPresentationService;
        private readonly StadiumsProvider _stadiumsProvider;
        private readonly UiObservableCollection<MatchViewModel> _matchesInConflicts = [];
        private readonly UiObservableCollection<MatchConflict> _conflicts = [];

        public MatchViewModel(Match item,
                              IMatchesStageViewModel stage,
                              MatchPresentationService matchPresentationService,
                              StadiumsProvider stadiumsProvider,
                              TeamsProvider teamsProvider) : base(item)
        {
            _matchPresentationService = matchPresentationService;
            _stadiumsProvider = stadiumsProvider;

            MatchesInConflicts = new(_matchesInConflicts);
            Conflicts = new(_conflicts);
            Stage = stage;
            WinnerTeam = new WinnerOfMatchTeamViewModel(item.GetWinnerTeam(), this);
            LooserTeam = new LooserOfMatchTeamViewModel(item.GetLooserTeam(), this);

            Home = new(item.WhenChanged(x => x.Home, (x, y) => (y, x.Home is not null ? teamsProvider.Get(x.Home.Team.Id) : null, teamsProvider.GetVirtualTeam(x.HomeTeam))), this, _matchPresentationService);
            Away = new(item.WhenChanged(x => x.Away, (x, y) => (y, x.Away is not null ? teamsProvider.Get(x.Away.Team.Id) : null, teamsProvider.GetVirtualTeam(x.AwayTeam))), this, _matchPresentationService);

            EditCommand = CommandsManager.Create(async () => await EditAsync().ConfigureAwait(false));
            RemoveCommand = CommandsManager.Create(async () => await RemoveAsync().ConfigureAwait(false));
            OpenCommand = CommandsManager.Create(async () => await OpenAsync().ConfigureAwait(false));
            StartCommand = CommandsManager.Create(async () => await StartAsync().ConfigureAwait(false), () => CanBe(MatchState.InProgress));
            SuspendCommand = CommandsManager.Create(async () => await SuspendAsync().ConfigureAwait(false), () => CanBe(MatchState.Suspended));
            PostponeCommand = CommandsManager.Create(async () => await PostponeAsync().ConfigureAwait(false), () => CanBe(MatchState.Postponed));
            CancelCommand = CommandsManager.Create(async () => await CancelAsync().ConfigureAwait(false), () => CanBe(MatchState.Cancelled));
            FinishCommand = CommandsManager.Create(async () => await FinishAsync().ConfigureAwait(false), () => CanBe(MatchState.Played));
            ResetCommand = CommandsManager.Create(async () => await ResetAsync().ConfigureAwait(false), () => CanBe(MatchState.None));
            RandomizeCommand = CommandsManager.Create(async () => await RandomizeAsync().ConfigureAwait(false), CanRandomize);
            InvertTeamsCommand = CommandsManager.Create(async () => await InvertTeamsAsync().ConfigureAwait(false), CanInvertTeams);
            RescheduleXMinutesCommand = CommandsManager.CreateNotNull<int>(async x => await RescheduleAsync(x, TimeUnit.Minute).ConfigureAwait(false), x => CanReschedule());
            RescheduleXHoursCommand = CommandsManager.CreateNotNull<int>(async x => await RescheduleAsync(x, TimeUnit.Hour).ConfigureAwait(false), x => CanReschedule());
            RescheduleCommand = CommandsManager.CreateNotNull<object[]>(async x => await RescheduleAsync(Convert.ToInt32(x[0]), (TimeUnit)x[1]).ConfigureAwait(false), x => x.Length == 2 && x[0] is double && x[1] is TimeUnit && CanReschedule());
            RescheduleAutomaticCommand = CommandsManager.Create(async () => await RescheduleAutomaticAsync().ConfigureAwait(false), CanRescheduleAutomatic);
            RescheduleAutomaticStadiumCommand = CommandsManager.Create(async () => await RescheduleAutomaticStadiumAsync().ConfigureAwait(false), CanRescheduleAutomaticStadium);

            var scoreChangedRequestedSubject = new Subject<bool>();
            Disposables.AddRange(
                [
                scoreChangedRequestedSubject.Throttle(50.Milliseconds()).Subscribe(_ =>
                {
                    RaiseScoreProperties();
                    ScoreChanged?.Invoke(this, EventArgs.Empty);
                }),
                Home.Goals.ToObservableChangeSet()
                          .Merge(Away.Goals.ToObservableChangeSet())
                          .SkipInitial()
                          .Subscribe(_ => scoreChangedRequestedSubject.OnNext(true)),
                Home.Shootouts.ToObservableChangeSet()
                              .Merge(Away.Shootouts.ToObservableChangeSet())
                              .SkipInitial()
                              .Subscribe(_ => scoreChangedRequestedSubject.OnNext(true)),
                Home.WhenPropertyChanged(x => x.IsWithdrawn, false)
                    .Merge(Away.WhenPropertyChanged(x => x.IsWithdrawn, false))
                    .Subscribe(_ => scoreChangedRequestedSubject.OnNext(true)),
                this.WhenPropertyChanged(x => x.AfterExtraTime, false).Subscribe(_ => scoreChangedRequestedSubject.OnNext(true)),
                this.WhenPropertyChanged(x => x.State).Subscribe(_ =>
                {
                    IsPlayed = State is MatchState.Played;
                    IsPlaying = State is MatchState.InProgress or MatchState.Suspended;
                    IsPostponed = State is MatchState.Postponed;
                    HasResult = Item.HasResult();
                    RaisePropertyChanged(nameof(CanBeWithdraw));
                    RaisePropertyChanged(nameof(CanBeRescheduled));
                }),
                this.WhenPropertyChanged(x => x.HasResult, false).Subscribe(_ => scoreChangedRequestedSubject.OnNext(true)),
                this.WhenPropertyChanged(x => x.IsPlayed, false).Subscribe(_ => scoreChangedRequestedSubject.OnNext(true)),
                this.WhenPropertyChanged(x => x.IsPlaying, false).Subscribe(_ => scoreChangedRequestedSubject.OnNext(true)),
                this.WhenPropertyChanged(x => x.Date, false).Subscribe(_ => RaiseDateProperties())
            ]);
        }

        public event EventHandler? ScoreChanged;

        public IMatchesStageViewModel Stage { get; set; }

        [UpdateOnCultureChanged]
        public string Name => (Stage.Matches.IndexOf(this) + 1).ToString(MyClubResources.MatchX);

        [UpdateOnCultureChanged]
        public string ShortName => (Stage.Matches.IndexOf(this) + 1).ToString(MyClubResources.MatchXAbbr);

        [UpdateOnCultureChanged]
        public string DisplayName => $"{Stage.Name} {Name}";

        [UpdateOnCultureChanged]
        public string DisplayShortName => $"{Stage.ShortName}{ShortName}";

        public WinnerOfMatchTeamViewModel WinnerTeam { get; }

        public LooserOfMatchTeamViewModel LooserTeam { get; }

        public MatchFormat Format => Item.Format;

        public MatchRules Rules => Item.Rules;

        public MatchOpponentViewModel Home { get; private set; }

        public MatchOpponentViewModel Away { get; private set; }

        public bool NeutralVenue => Item.IsNeutralStadium;

        public StadiumViewModel? Stadium => Item.Stadium is not null ? _stadiumsProvider.Get(Item.Stadium.Id) : null;

        public bool AfterExtraTime => Item.Format.ExtraTimeIsEnabled && Item.AfterExtraTime;

        public bool AfterShootouts => Item.Format.ShootoutIsEnabled && Item.IsDraw() && (Home.ShootoutScore > 0 || Away.ShootoutScore > 0);

        public DateOnly DateOfDay => Date.ToDate();

        public DateTime Date => Item.Date.ToCurrentTime();

        public DateTime StartDate => Date;

        public DateTime EndDate => Date.AddFluentTimeSpan(GetTotalTime());

        public MatchState State => Item.State;

        public bool IsPlayed { get; private set; }

        public bool IsPlaying { get; private set; }

        public bool IsPostponed { get; private set; }

        public bool HasResult { get; private set; }

        public bool CanBeWithdraw => CanDoWithdraw();

        public bool CanBeRescheduled => CanReschedule();

        public ReadOnlyObservableCollection<MatchViewModel> MatchesInConflicts { get; }

        public ReadOnlyObservableCollection<MatchConflict> Conflicts { get; }

        public ICommand OpenCommand { get; }

        public ICommand EditCommand { get; }

        public ICommand ResetCommand { get; }

        public ICommand StartCommand { get; }

        public ICommand PostponeCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand SuspendCommand { get; }

        public ICommand FinishCommand { get; }

        public ICommand RandomizeCommand { get; }

        public ICommand InvertTeamsCommand { get; }

        public ICommand RemoveCommand { get; }

        public ICommand RescheduleXMinutesCommand { get; }

        public ICommand RescheduleXHoursCommand { get; }

        public ICommand RescheduleAutomaticCommand { get; }

        public ICommand RescheduleAutomaticStadiumCommand { get; }

        public ICommand RescheduleCommand { get; }

        public bool CanBe(MatchState state)
            => state switch
            {
                MatchState.None => true,
                MatchState.InProgress => State is MatchState.None or MatchState.Suspended or MatchState.Postponed,
                MatchState.Suspended => State is MatchState.InProgress,
                MatchState.Played => State is MatchState.None or MatchState.Suspended or MatchState.Postponed or MatchState.InProgress,
                MatchState.Postponed => State is MatchState.None or MatchState.Suspended,
                MatchState.Cancelled => State is MatchState.None or MatchState.Postponed && Stage.CanCancelMatch(),
                _ => false,
            };

        public bool CanReschedule() => State is MatchState.None or MatchState.Postponed;

        public bool CanRescheduleAutomatic() => CanReschedule() && Item.CanAutomaticReschedule();

        public bool CanRescheduleAutomaticStadium() => CanReschedule() && Item.CanAutomaticRescheduleVenue();

        public bool CanRandomize() => State is MatchState.None or MatchState.InProgress or MatchState.Played or MatchState.Suspended or MatchState.Postponed;

        public bool CanInvertTeams() => State is MatchState.None;

        public bool CanDoWithdraw() => State is MatchState.None or MatchState.InProgress or MatchState.Suspended;

        public bool Participate(IVirtualTeamViewModel team) => Item.Participate(team.Id);

        public bool Participate(Guid teamId) => Item.Participate(teamId);

        public IVirtualTeamViewModel? GetOpponentOf(IVirtualTeamViewModel team) => Home.Team == team ? Away.Team : Away.Team == team ? Home.Team : null;

        public Result GetResultOf(IVirtualTeamViewModel team) => Item.GetResultOf(team.Id);

        public ExtendedResult GetDetailledResultOf(IVirtualTeamViewModel team) => Item.GetExtendedResultOf(team.Id);

        public IVirtualTeamViewModel? GetWinner() => GetResultOf(Home.Team) == Result.Won ? Home.Team : GetResultOf(Away.Team) == Result.Won ? Away.Team : null;

        public IVirtualTeamViewModel? GetLooser() => GetResultOf(Home.Team) == Result.Lost ? Home.Team : GetResultOf(Away.Team) == Result.Lost ? Away.Team : null;

        public bool IsWonBy(IVirtualTeamViewModel team) => Item.IsWonBy(team.Id);

        public bool IsLostBy(IVirtualTeamViewModel team) => Item.IsLostBy(team.Id);

        public int GoalsFor(IVirtualTeamViewModel team) => Item.GoalsFor(team.Id);

        public int GoalsAgainst(IVirtualTeamViewModel team) => Item.GoalsAgainst(team.Id);

        public bool IsWithdrawn(IVirtualTeamViewModel team) => Item.IsWithdrawn(team.Id);

        public TimeSpan GetTotalTime() => Item.Format.RegulationTime.Duration * Item.Format.RegulationTime.Number + 15.Minutes();

        public Period GetPeriod() => new(Date, EndDate);

        public async Task OpenAsync() => await _matchPresentationService.OpenAsync(this).ConfigureAwait(false);

        public async Task EditAsync() => await _matchPresentationService.EditAsync(this).ConfigureAwait(false);

        public async Task StartAsync() => await _matchPresentationService.StartAsync(this).ConfigureAwait(false);

        public async Task ResetAsync() => await _matchPresentationService.ResetAsync(this).ConfigureAwait(false);

        public async Task CancelAsync() => await _matchPresentationService.CancelAsync(this).ConfigureAwait(false);

        public async Task PostponeAsync() => await _matchPresentationService.PostponeAsync(this).ConfigureAwait(false);

        public async Task SuspendAsync() => await _matchPresentationService.SuspendAsync(this).ConfigureAwait(false);

        public async Task FinishAsync() => await _matchPresentationService.FinishAsync(this).ConfigureAwait(false);

        public async Task RescheduleAsync(int offset, TimeUnit timeUnit) => await _matchPresentationService.RescheduleAsync(this, offset, timeUnit).ConfigureAwait(false);

        public async Task RescheduleAutomaticAsync() => await _matchPresentationService.RescheduleAutomaticAsync(this).ConfigureAwait(false);

        public async Task RescheduleAutomaticStadiumAsync() => await _matchPresentationService.RescheduleAutomaticStadiumAsync(this).ConfigureAwait(false);

        public async Task RandomizeAsync() => await _matchPresentationService.RandomizeAsync(this).ConfigureAwait(false);

        public async Task InvertTeamsAsync() => await _matchPresentationService.InvertTeamsAsync(this).ConfigureAwait(false);

        public async Task RemoveAsync() => await _matchPresentationService.RemoveAsync([this]).ConfigureAwait(false);

        public void SetConflicts(IEnumerable<MatchConflict> conflicts)
        {
            _conflicts.Set(conflicts);
            _matchesInConflicts.Set(conflicts.Where(x => x.Match is not null).Select(x => x.Match!).Distinct());
        }

        private void RaiseScoreProperties()
        {
            RaisePropertyChanged(nameof(AfterExtraTime));
            RaisePropertyChanged(nameof(AfterShootouts));
            RaisePropertyChanged(nameof(CanBeWithdraw));
        }

        private void RaiseDateProperties()
        {
            RaisePropertyChanged(nameof(IAppointment.StartDate));
            RaisePropertyChanged(nameof(EndDate));
            RaisePropertyChanged(nameof(DateOfDay));
        }

        protected override void Cleanup()
        {
            Home.Dispose();
            Away.Dispose();
            base.Cleanup();
        }
    }

    internal class MatchConflict(ConflictType type, MatchViewModel? match)
    {
        public ConflictType Type { get; } = type;

        public MatchViewModel? Match { get; } = match;
    }
}
