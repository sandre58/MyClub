// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.UI.Collections;
using MyNet.UI.Resources;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using PropertyChanged;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    public enum DrawManagement
    {
        None,

        ExtraTime,

        Shootout
    }

    internal class MatchEditionViewModel : EntityEditionViewModel<Match, MatchDto, MatchService>
    {
        private readonly AvailibilityCheckingService _availibilityCheckingService;
        private readonly UiObservableCollection<IVirtualTeamViewModel> _availableTeams = [];

        public MatchEditionViewModel(MatchService matchService,
                                     AvailibilityCheckingService availibilityCheckingService,
                                     StadiumsProvider stadiumsProvider) : base(matchService)
        {
            _availibilityCheckingService = availibilityCheckingService;
            StadiumSelection = new ListViewModel<StadiumWrapper>(stadiumsProvider.Connect().Transform(x => new StadiumWrapper(x)));
            AvailableTeams = new(_availableTeams);
            Home = new(MyClubResources.HomeTeam);
            Away = new(MyClubResources.AwayTeam);

            Mode = ScreenMode.Edition;

            ValidationRules.Add<MatchEditionViewModel, EditableMatchOpponentViewModel?>(x => x.Home, () => MessageResources.FieldXMustBeDifferentOfFieldYError.FormatWith(MyClubResources.HomeTeam, MyClubResources.AwayTeam), x => x?.Team is null || Away.Team is null || x.Team.Id != Away.Team.Id);

            Disposables.AddRange(
            [
                // Home
                Home.WhenPropertyChanged(x => x.Team, false).Subscribe(x =>
                {
                    if (!IsModifiedSuspender.IsSuspended && Mode == ScreenMode.Creation && !NeutralVenue && x.Value is TeamViewModel homeTeam)
                        StadiumSelection.SelectedItem = homeTeam.Stadium?.Id is Guid id ? StadiumSelection.GetByIdOrDefault(id) : null;
                }),
                Home.Goals.ToObservableChangeSet().Subscribe(_ => OnScoreChanged()),
                Home.Shootout.ToObservableChangeSet().Subscribe(_ => OnShootoutChanged()),
                Home.WhenPropertyChanged(x => x.IsWithdrawn, false).Subscribe(x =>
                {
                    if(IsModifiedSuspender.IsSuspended) return;

                     x.Value.IfTrue(() => DoWithdraw(Home, Away), OnScoreChanged);
                }),

                // Away
                Away.Goals.ToObservableChangeSet().Subscribe(_ => OnScoreChanged()),
                Away.Shootout.ToObservableChangeSet().Subscribe(_ => OnShootoutChanged()),
                Away.WhenPropertyChanged(x => x.IsWithdrawn, false).Subscribe(x =>
                {
                    if(IsModifiedSuspender.IsSuspended) return;

                    x.Value.IfTrue(() => DoWithdraw(Away, Home), OnScoreChanged);
                }),

                // Both
                Home.WhenPropertyChanged(x => x.Team, false).Merge(Away.WhenPropertyChanged(x => x.Team, false)).Subscribe(_ =>
                {
                    ValidateOriginDateAvaibility();
                    ValidatePostponedDateAvaibility();

                    CanEditScore = Home.ComputedTeam is not null && Away.ComputedTeam is not null;
                }),

                // DrawManagement
                this.WhenPropertyChanged(x => x.DrawManagement, false).Subscribe(_ => OnScoreChanged()),

                // State
                this.WhenPropertyChanged(x => x.State, false).Subscribe(_ =>
                {
                    if(IsModifiedSuspender.IsSuspended) return;

                    switch(State)
                    {
                        case MatchState.None:
                        case MatchState.Cancelled:
                        case MatchState.Postponed:
                            ResetScore();
                            break;
                        default:
                            break;

                    }

                    if(State == MatchState.Postponed && PostponedState != PostponedState.UnknownDate)
                        PostponedState = PostponedState.UnknownDate;
                    else if(State != MatchState.Postponed && PostponedState == PostponedState.UnknownDate)
                        PostponedState = PostponedState.None;
                }),
                this.WhenPropertyChanged(x => x.PostponedState, false).Subscribe(x =>
                {
                    if(x.Value == PostponedState.UnknownDate && State != MatchState.Postponed)
                        State = MatchState.Postponed;
                    else if(x.Value != PostponedState.UnknownDate && State == MatchState.Postponed)
                        State = MatchState.None;

                    ValidateStadiumsAvaibility();
                }),

                // Date
                CurrentDate.WhenPropertyChanged(x => x.DateTime).Subscribe(x =>
                {
                    ValidateOriginDateAvaibility();
                    ValidateStadiumsAvaibility();
                    ScheduleAutomatic = false;
                }),
                PostponedDateTime.WhenPropertyChanged(x => x.DateTime).Subscribe(x =>
                {
                    ValidatePostponedDateAvaibility();
                    ValidateStadiumsAvaibility();

                    ScheduleAutomatic = false;
                }),

                // Stadium
                StadiumSelection.WhenPropertyChanged(x => x.SelectedItem, false).Subscribe(_ => ValidateStadiumsAvaibility()),
                StadiumSelection.WhenPropertyChanged(x => x.SelectedItem).Subscribe(_ => ScheduleStadiumAutomatic = false),
            ]);
        }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public AvailabilityCheck DateAvailability { get; private set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public AvailabilityCheck PostponedDateAvailability { get; private set; }

        [IsRequired]
        [DoNotCheckEquality]
        public IMatchParentViewModel? Parent { get; private set; }

        public EditableDateTime CurrentDate { get; set; } = new();

        public EditableDateTime PostponedDateTime { get; set; } = new(false);

        public bool ScheduleAutomatic { get; set; }

        public bool ScheduleStadiumAutomatic { get; set; }

        public bool CanScheduleAutomatic { get; set; }

        public bool CanScheduleStadiumAutomatic { get; set; }

        public PostponedState PostponedState { get; set; }

        public DrawManagement DrawManagement { get; set; }

        [IsRequired]
        [Display(Name = nameof(State), ResourceType = typeof(MyClubResources))]
        public MatchState State { get; set; }

        [IsRequired]
        [Display(Name = nameof(NeutralVenue), ResourceType = typeof(MyClubResources))]
        public bool NeutralVenue { get; set; }

        public EditableMatchFormatViewModel MatchFormat { get; } = new();

        public EditableMatchRulesViewModel MatchRules { get; } = new();

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool ShowGoals { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool CanCancel { get; private set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool CanEditFormat { get; private set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool CanEditRules { get; private set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool CanEditScore { get; private set; }

        public EditableMatchOpponentViewModel Home { get; }

        public EditableMatchOpponentViewModel Away { get; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public ReadOnlyObservableCollection<IVirtualTeamViewModel> AvailableTeams { get; }

        [CanBeValidated]
        [CanSetIsModified]
        public ListViewModel<StadiumWrapper> StadiumSelection { get; }

        private void DoWithdraw(EditableMatchOpponentViewModel teamWidrawn, EditableMatchOpponentViewModel otherTeam)
        {
            State = MatchState.Played;
            DrawManagement = DrawManagement.None;
            teamWidrawn.DoWithdraw();
            otherTeam.WinByWithdraw();
        }

        private void ResetScore()
        {
            using (IsModifiedSuspender.Suspend())
            {
                Home.ResetScore();
                Away.ResetScore();
                DrawManagement = DrawManagement.None;
                ShowGoals = true;
            }
        }

        [SuppressPropertyChangedWarnings]
        private void OnScoreChanged()
        {
            if (IsModifiedSuspender.IsSuspended) return;

            if (State == MatchState.None) State = MatchState.Played;

            ShowGoals = true;
        }

        [SuppressPropertyChangedWarnings]
        private void OnShootoutChanged() => ShowGoals = false;

        private void ValidateOriginDateAvaibility()
        {
            if (IsModifiedSuspender.IsSuspended) return;

            if (!CurrentDate.HasValue)
            {
                DateAvailability = AvailabilityCheck.Unknown;
                return;
            }

            DateAvailability = CheckTeamsAvaibility(CurrentDate.ToUtcOrDefault());
        }

        private void ValidatePostponedDateAvaibility()
        {
            if (IsModifiedSuspender.IsSuspended) return;

            if (!PostponedDateTime.HasValue)
            {
                PostponedDateAvailability = AvailabilityCheck.Unknown;
                return;
            }

            PostponedDateAvailability = CheckTeamsAvaibility(PostponedDateTime.ToUtcOrDefault());
        }

        private void ValidateStadiumsAvaibility()
        {
            if (IsModifiedSuspender.IsSuspended) return;

            DateTime? date = null;

            if (PostponedState == PostponedState.SpecifiedDate && PostponedDateTime.HasValue)
                date = PostponedDateTime.ToUtcOrDefault();
            else if (CurrentDate.HasValue)
                date = CurrentDate.ToUtcOrDefault();

            foreach (var item in StadiumSelection.Items)
                item.Availability = date.HasValue ? CheckStadiumAvaibility(item.Stadium.Id, date.Value) : AvailabilityCheck.Unknown;
        }

        private AvailabilityCheck CheckTeamsAvaibility(DateTime utcDate)
            => Home.Team is null && Away.Team is null
                ? AvailabilityCheck.Unknown
                : _availibilityCheckingService.GetTeamsAvaibility(new[] { Home.Team?.Id, Away.Team?.Id, Home.ComputedTeam?.Id, Away.ComputedTeam?.Id }.NotNull().Distinct().OfType<Guid>(), new Period(utcDate, utcDate.AddFluentTimeSpan(MatchFormat.Create().GetFullTime())), [ItemId ?? Guid.Empty]);

        private AvailabilityCheck CheckStadiumAvaibility(Guid stadiumId, DateTime utcDate)
            => _availibilityCheckingService.GetStadiumAvaibility(stadiumId, new Period(utcDate, utcDate.AddFluentTimeSpan(MatchFormat.Create().GetFullTime())), [ItemId ?? Guid.Empty]);

        public void Load(MatchViewModel match)
        {
            Parent = match.Parent;
            _availableTeams.Set(Parent.GetAvailableTeams());
            CanScheduleAutomatic = Parent.CanAutomaticReschedule();
            CanScheduleStadiumAutomatic = Parent.CanAutomaticRescheduleVenue();
            Load(match.Id);
        }

        public void New(IMatchParentViewModel parent, Action? initialize = null)
        {
            Parent = parent;
            _availableTeams.Set(Parent.GetAvailableTeams());
            CanScheduleAutomatic = Parent?.CanAutomaticReschedule() ?? false;
            CanScheduleStadiumAutomatic = Parent?.CanAutomaticRescheduleVenue() ?? false;
            CanCancel = Parent?.CanCancelMatch() ?? true;
            New(initialize);
        }

        public override void Refresh()
        {
            base.Refresh();

            ValidateOriginDateAvaibility();
            ValidatePostponedDateAvaibility();
            ValidateStadiumsAvaibility();
        }

        protected override void ResetItem()
        {
            if (Parent is not null)
            {
                var defaultValues = CrudService.New(Parent.Id, Parent.Date);
                MatchFormat.Load(defaultValues.Format ?? Domain.MatchAggregate.MatchFormat.Default);
                MatchRules.Load(defaultValues.Rules ?? Domain.MatchAggregate.MatchRules.Default);
                CurrentDate.Load(defaultValues.Date);
                Home.Reset();
                Away.Reset();
                StadiumSelection.SelectedItem = defaultValues.Stadium?.Id is not null ? StadiumSelection.Items.GetByIdOrDefault(defaultValues.Stadium.Id.Value) : null;
                NeutralVenue = defaultValues.IsNeutralStadium;
                DrawManagement = defaultValues.AfterExtraTime ? DrawManagement.ExtraTime : DrawManagement.None;
                State = defaultValues.State;
                PostponedState = PostponedState.None;
                PostponedDateTime.Clear();
                ScheduleAutomatic = false;
                ScheduleStadiumAutomatic = false;
                ShowGoals = true;
            }
        }

        protected override MatchDto ToDto()
            => new()
            {
                Id = ItemId,
                StageId = Parent?.Id ?? throw new InvalidOperationException("Stage cannot be null"),
                HomeTeamId = Home.Team?.Id ?? throw new InvalidOperationException("HomeTeam cannot be null"),
                AwayTeamId = Away.Team?.Id ?? throw new InvalidOperationException("AwayTeam cannot be null"),
                Date = CurrentDate.ToUtcOrDefault(),
                Format = CanEditFormat && MatchFormat.IsModified() ? MatchFormat.Create() : null,
                Rules = CanEditRules && MatchRules.IsModified() ? MatchRules.Create() : null,
                IsNeutralStadium = NeutralVenue,
                Stadium = StadiumSelection.SelectedItem is not null ? new StadiumDto
                {
                    Id = StadiumSelection.SelectedItem.Id,
                    Name = StadiumSelection.SelectedItem.Stadium.Name,
                    Ground = StadiumSelection.SelectedItem.Stadium.Ground,
                    Address = StadiumSelection.SelectedItem.Stadium.Address,
                } : null,
                HomeGoals = State is MatchState.Played or MatchState.Suspended or MatchState.InProgress
                            ? Home.Goals.Where(x => x.Type.HasValue).Select(x => x.ToDto()).ToList()
                            : [],
                AwayGoals = State is MatchState.Played or MatchState.Suspended or MatchState.InProgress
                            ? Away.Goals.Where(x => x.Type.HasValue).Select(x => x.ToDto()).ToList()
                            : [],
                HomeShootout = DrawManagement == DrawManagement.Shootout
                            ? Home.Shootout.Where(x => x.Result.HasValue).Select(x => x.ToDto()).ToList()
                            : [],
                AwayShootout = DrawManagement == DrawManagement.Shootout
                            ? Away.Shootout.Where(x => x.Result.HasValue).Select(x => x.ToDto()).ToList()
                            : [],
                HomeCards = State is MatchState.Played or MatchState.Suspended or MatchState.InProgress
                            ? Home.Cards.Where(x => x.Color.HasValue).Select(x => x.ToDto()).ToList()
                            : [],
                AwayCards = State is MatchState.Played or MatchState.Suspended or MatchState.InProgress
                            ? Away.Cards.Where(x => x.Color.HasValue).Select(x => x.ToDto()).ToList()
                            : [],
                HomeIsWithdrawn = Home.IsWithdrawn,
                AwayIsWithdrawn = Away.IsWithdrawn,
                AfterExtraTime = DrawManagement == DrawManagement.ExtraTime,
                State = State,
                PostponedDate = PostponedState == PostponedState.SpecifiedDate ? PostponedDateTime.ToUtc() : null,
                ScheduleStadiumAutomatic = CanScheduleStadiumAutomatic && ScheduleStadiumAutomatic,
                ScheduleAutomatic = CanScheduleAutomatic && ScheduleAutomatic
            };

        protected override void RefreshFrom(Match item)
        {
            if (Parent is not null)
            {
                Home.Load(Parent.GetAvailableTeams().GetById(item.HomeTeam.Id), item.State, item.Home);
                Away.Load(Parent.GetAvailableTeams().GetById(item.AwayTeam.Id), item.State, item.Away);

                StadiumSelection.SelectedItem = item.Stadium?.Id is not null ? StadiumSelection.Items.GetByIdOrDefault(item.Stadium.Id) : null;
                CurrentDate.Load(item.OriginDate);
                MatchFormat.Load(item.Format);
                MatchRules.Load(item.Rules);
                NeutralVenue = item.IsNeutralStadium;
                DrawManagement = item.AfterExtraTime ? DrawManagement.ExtraTime : item.Format.ShootoutIsEnabled && (item.Home?.Shootout.Count > 0 || item.Away?.Shootout.Count > 0) ? DrawManagement.Shootout : DrawManagement.None;
                State = item.State;
                if (item.OriginDate == item.Date)
                    PostponedDateTime.Clear();
                else
                    PostponedDateTime.Load(item.Date);
                PostponedState = !PostponedDateTime.HasValue && item.State != MatchState.Postponed ? PostponedState.None : PostponedDateTime.HasValue ? PostponedState.SpecifiedDate : PostponedState.UnknownDate;
                ScheduleAutomatic = false;
                ScheduleStadiumAutomatic = false;
                ShowGoals = true;
            }
        }

        protected override void Cleanup()
        {
            Home.Dispose();
            Away.Dispose();
            CurrentDate.Dispose();
            PostponedDateTime.Dispose();
            MatchFormat.Dispose();
            MatchRules.Dispose();
            base.Cleanup();
        }
    }
}
