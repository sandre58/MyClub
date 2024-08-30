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
using MyNet.Observable.Translatables;
using MyNet.UI.Collections;
using MyNet.UI.Resources;
using MyNet.UI.Threading;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using PropertyChanged;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class MatchEditionViewModel : EntityEditionViewModel<Match, MatchDto, MatchService>
    {
        private readonly AvailibilityCheckingService _availibilityCheckingService;
        private readonly ReadOnlyObservableCollection<IMatchParent> _parents;
        private readonly UiObservableCollection<ITeamViewModel> _availableTeams = [];

        public MatchEditionViewModel(MatchService matchService,
                                     AvailibilityCheckingService availibilityCheckingService,
                                     StadiumsProvider stadiumsProvider,
                                     MatchdaysProvider matchdaysProvider) : base(matchService)
        {
            _availibilityCheckingService = availibilityCheckingService;
            StadiumSelection = new ListViewModel<StadiumWrapper>(stadiumsProvider.Connect().Transform(x => new StadiumWrapper(x)));
            AvailableTeams = new(_availableTeams);

            Mode = ScreenMode.Edition;

            ValidationRules.Add<MatchEditionViewModel, ITeamViewModel?>(x => x.HomeTeam, MessageResources.FieldXMustBeDifferentOfFieldYError.FormatWith(MyClubResources.HomeTeam, MyClubResources.AwayTeam), x => x is null || AwayTeam is null || x.Id != AwayTeam.Id);

            Disposables.AddRange(
            [
                matchdaysProvider.ConnectById().Transform(x => (IMatchParent)x).SortBy(x => x.Date).Bind(out _parents).ObserveOn(Scheduler.UI).Subscribe(),
                HomeScore.WhenPropertyChanged(x => x.Value, false).Subscribe(_ => OnScoreChanged()),
                AwayScore.WhenPropertyChanged(x => x.Value, false).Subscribe(_ => OnScoreChanged()),
                this.WhenPropertyChanged(x => x.HomeIsWithdrawn, false).Subscribe(_ =>
                {
                    if(IsModifiedSuspender.IsSuspended) return;

                    if(HomeIsWithdrawn)
                    {
                        AwayIsWithdrawn = false;
                        HomeScore.Value = 0;
                        AwayScore.Value = 3;
                        State = MatchState.Played;
                    }
                }),
                this.WhenPropertyChanged(x => x.AwayIsWithdrawn, false).Subscribe(_ =>
                {
                    if(IsModifiedSuspender.IsSuspended) return;

                    if(AwayIsWithdrawn)
                    {
                        HomeIsWithdrawn = false;
                        HomeScore.Value = 3;
                        AwayScore.Value = 0;
                        State = MatchState.Played;
                    }
                }),
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
                this.WhenPropertyChanged(x => x.Parent, false).Subscribe(x =>
                {
                    CanEditFormat = x.Value?.CanEditMatchFormat() ?? false;
                    CanScheduleAutomatic = x.Value?.CanAutomaticReschedule() ?? false;
                    CanScheduleStadiumAutomatic = x.Value?.CanAutomaticRescheduleVenue() ?? false;
                    _availableTeams.Set(x.Value?.GetAvailableTeams() ?? []);

                    if(IsModifiedSuspender.IsSuspended) return;

                    Reset();
                }),
                this.WhenPropertyChanged(x => x.HomeTeam, false).Subscribe(x =>
                {
                    if (!IsModifiedSuspender.IsSuspended && Mode == ScreenMode.Creation && !NeutralVenue)
                        StadiumSelection.SelectedItem = x.Value?.Stadium?.Id is Guid id ? StadiumSelection.GetByIdOrDefault(id) : null;

                    ValidateOriginDateAvaibility();
                    ValidatePostponedDateAvaibility();
                }),
                this.WhenPropertyChanged(x => x.AwayTeam, false).Subscribe(_ =>
                {
                    ValidateOriginDateAvaibility();
                    ValidatePostponedDateAvaibility();
                }),
                StadiumSelection.WhenPropertyChanged(x => x.SelectedItem, false).Subscribe(_ => ValidateStadiumsAvaibility()),
                DateTime.WhenPropertyChanged(x => x.DateTime).Subscribe(x =>
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
                StadiumSelection.WhenPropertyChanged(x => x.SelectedItem).Subscribe(_ => ScheduleStadiumAutomatic = false)
            ]);
        }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public AvailabilityCheck DateAvailability { get; private set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public AvailabilityCheck PostponedDateAvailability { get; private set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public ReadOnlyObservableCollection<IMatchParent> Parents => _parents;

        [IsRequired]
        [DoNotCheckEquality]
        public IMatchParent? Parent { get; set; }

        public EditableDateTime DateTime { get; set; } = new();

        public EditableDateTime PostponedDateTime { get; set; } = new(false);

        public bool ScheduleAutomatic { get; set; }

        public bool ScheduleStadiumAutomatic { get; set; }

        public bool CanScheduleAutomatic { get; set; }

        public bool CanScheduleStadiumAutomatic { get; set; }

        public PostponedState PostponedState { get; set; }

        [IsRequired]
        [Display(Name = nameof(State), ResourceType = typeof(MyClubResources))]
        public MatchState State { get; set; }

        [IsRequired]
        [Display(Name = nameof(NeutralVenue), ResourceType = typeof(MyClubResources))]
        public bool NeutralVenue { get; set; }

        public EditableMatchFormatViewModel MatchFormat { get; } = new();

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool CanEditFormat { get; private set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool CanEditExtraTimeOrShootout => (MatchFormat.ExtraTimeIsEnabled || MatchFormat.ShootoutsIsEnabled) && HasDraw;

        [IsRequired]
        [Display(Name = nameof(HomeTeam), ResourceType = typeof(MyClubResources))]
        public ITeamViewModel? HomeTeam { get; set; }

        [IsRequired]
        [Display(Name = nameof(AwayTeam), ResourceType = typeof(MyClubResources))]
        public ITeamViewModel? AwayTeam { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public ReadOnlyObservableCollection<ITeamViewModel> AvailableTeams { get; }

        [CanBeValidated]
        [CanSetIsModified]
        public ListViewModel<StadiumWrapper> StadiumSelection { get; }

        [Display(Name = nameof(HomeScore), ResourceType = typeof(MyClubResources))]
        public AcceptableValue<int> HomeScore { get; } = new AcceptableValue<int>(Match.AcceptableRangeScore);

        [Display(Name = nameof(AwayScore), ResourceType = typeof(MyClubResources))]
        public AcceptableValue<int> AwayScore { get; } = new AcceptableValue<int>(Match.AcceptableRangeScore);

        [Display(Name = nameof(HomeScore), ResourceType = typeof(MyClubResources))]
        public AcceptableValue<int> HomeShootoutScore { get; } = new AcceptableValue<int>(Match.AcceptableRangeScore);

        [Display(Name = nameof(AwayScore), ResourceType = typeof(MyClubResources))]
        public AcceptableValue<int> AwayShootoutScore { get; } = new AcceptableValue<int>(Match.AcceptableRangeScore);

        [Display(Name = nameof(AfterExtraTime), ResourceType = typeof(MyClubResources))]
        public bool AfterExtraTime { get; set; }

        [Display(Name = nameof(HomeIsWithdrawn), ResourceType = typeof(MyClubResources))]
        public bool HomeIsWithdrawn { get; set; }

        [Display(Name = nameof(AwayIsWithdrawn), ResourceType = typeof(MyClubResources))]
        public bool AwayIsWithdrawn { get; set; }

        public bool HasDraw => HomeScore == AwayScore && !HomeIsWithdrawn && !AwayIsWithdrawn;

        private void ResetScore()
        {
            using (IsModifiedSuspender.Suspend())
            {
                HomeScore.Reset();
                AwayScore.Reset();
                HomeShootoutScore.Reset();
                AwayShootoutScore.Reset();
                HomeIsWithdrawn = false;
                AwayIsWithdrawn = false;
                AfterExtraTime = false;
            }
        }

        [SuppressPropertyChangedWarnings]
        private void OnScoreChanged()
        {
            if (IsModifiedSuspender.IsSuspended) return;

            if (State == MatchState.None) State = MatchState.Played;

            RaisePropertyChanged(nameof(HasDraw));
            RaisePropertyChanged(nameof(CanEditExtraTimeOrShootout));
            if (HasDraw)
                AfterExtraTime = false;
        }

        private void ValidateOriginDateAvaibility()
        {
            if (IsModifiedSuspender.IsSuspended) return;

            if (!DateTime.HasValue)
            {
                DateAvailability = AvailabilityCheck.Unknown;
                return;
            }

            DateAvailability = CheckTeamsAvaibility(DateTime.ToUtcOrDefault());
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
            else if (DateTime.HasValue)
                date = DateTime.ToUtcOrDefault();

            foreach (var item in StadiumSelection.Items)
                item.Availability = date.HasValue ? CheckStadiumAvaibility(item.Stadium.Id, date.Value) : AvailabilityCheck.Unknown;
        }

        private AvailabilityCheck CheckTeamsAvaibility(DateTime utcDate)
            => HomeTeam is null && AwayTeam is null
                ? AvailabilityCheck.Unknown
                : _availibilityCheckingService.GetTeamsAvaibility(new[] { HomeTeam?.Id, AwayTeam?.Id }.NotNull().OfType<Guid>(), new Period(utcDate, utcDate.AddFluentTimeSpan(MatchFormat.Create().GetFullTime())), [ItemId ?? Guid.Empty]);

        private AvailabilityCheck CheckStadiumAvaibility(Guid stadiumId, DateTime utcDate)
            => _availibilityCheckingService.GetStadiumAvaibility(stadiumId, new Period(utcDate, utcDate.AddFluentTimeSpan(MatchFormat.Create().GetFullTime())), [ItemId ?? Guid.Empty]);

        public void Load(MatchViewModel match)
        {
            Parent = match.Parent;
            CanScheduleAutomatic = match.Parent.CanAutomaticReschedule();
            CanScheduleStadiumAutomatic = match.Parent.CanAutomaticRescheduleVenue();
            Load(match.Id);
        }

        public void New(IMatchParent? parent = null, Action? initialize = null)
        {
            Parent = parent ?? _parents.LastOrDefault();
            CanScheduleAutomatic = Parent?.CanAutomaticReschedule() ?? false;
            CanScheduleStadiumAutomatic = Parent?.CanAutomaticRescheduleVenue() ?? false;
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
                DateTime.Load(defaultValues.Date);
                HomeTeam = null;
                AwayTeam = null;
                StadiumSelection.SelectedItem = defaultValues.Stadium?.Id is not null ? StadiumSelection.Items.GetByIdOrDefault(defaultValues.Stadium.Id.Value) : null;
                NeutralVenue = defaultValues.IsNeutralStadium;
                HomeScore.Value = defaultValues.HomeScore;
                AwayScore.Value = defaultValues.AwayScore;
                HomeShootoutScore.Value = defaultValues.HomeShootoutScore;
                AwayShootoutScore.Value = defaultValues.AwayShootoutScore;
                HomeIsWithdrawn = defaultValues.HomeIsWithdrawn;
                AwayIsWithdrawn = defaultValues.AwayIsWithdrawn;
                AfterExtraTime = defaultValues.AfterExtraTime;
                State = defaultValues.State;
                PostponedState = PostponedState.None;
                PostponedDateTime.Clear();
                ScheduleAutomatic = false;
                ScheduleStadiumAutomatic = false;
            }
        }

        protected override MatchDto ToDto()
            => new()
            {
                Id = ItemId,
                ParentId = Parent?.Id ?? throw new InvalidOperationException("Parent cannot be null"),
                HomeTeamId = HomeTeam?.Id ?? throw new InvalidOperationException("HomeTeam cannot be null"),
                AwayTeamId = AwayTeam?.Id ?? throw new InvalidOperationException("AwayTeam cannot be null"),
                Date = DateTime.ToUtcOrDefault(),
                Format = CanEditFormat && MatchFormat.IsModified() ? MatchFormat.Create() : null,
                IsNeutralStadium = NeutralVenue,
                Stadium = StadiumSelection.SelectedItem is not null ? new StadiumDto
                {
                    Id = StadiumSelection.SelectedItem.Id,
                    Name = StadiumSelection.SelectedItem.Stadium.Name,
                    Ground = StadiumSelection.SelectedItem.Stadium.Ground,
                    Address = StadiumSelection.SelectedItem.Stadium.Address,
                } : null,
                HomeScore = HomeScore.Value,
                AwayScore = AwayScore.Value,
                HomeShootoutScore = HomeShootoutScore.Value,
                AwayShootoutScore = AwayShootoutScore.Value,
                HomeIsWithdrawn = HomeIsWithdrawn,
                AwayIsWithdrawn = AwayIsWithdrawn,
                AfterExtraTime = AfterExtraTime,
                State = State,
                PostponedDate = PostponedState == PostponedState.SpecifiedDate ? PostponedDateTime.ToUtc() : null,
                ScheduleStadiumAutomatic = CanScheduleStadiumAutomatic && ScheduleStadiumAutomatic,
                ScheduleAutomatic = CanScheduleAutomatic && ScheduleAutomatic
            };

        protected override void RefreshFrom(Match item)
        {
            if (Parent is not null)
            {
                HomeTeam = Parent.GetAvailableTeams().GetById(item.HomeTeam.Id);
                AwayTeam = Parent.GetAvailableTeams().GetById(item.AwayTeam.Id);
                StadiumSelection.SelectedItem = item.Stadium?.Id is not null ? StadiumSelection.Items.GetByIdOrDefault(item.Stadium.Id) : null;
                DateTime.Load(item.OriginDate);
                MatchFormat.Load(item.Format);
                NeutralVenue = item.IsNeutralStadium;
                HomeScore.Value = item.State == MatchState.InProgress || item.State == MatchState.Suspended || item.State == MatchState.Played ? item.Home.GetScore() : null;
                AwayScore.Value = item.State == MatchState.InProgress || item.State == MatchState.Suspended || item.State == MatchState.Played ? item.Away.GetScore() : null;
                HomeShootoutScore.Value = item.State == MatchState.InProgress || item.State == MatchState.Suspended || item.State == MatchState.Played ? item.Home.GetShootoutScore() : null;
                AwayShootoutScore.Value = item.State == MatchState.InProgress || item.State == MatchState.Suspended || item.State == MatchState.Played ? item.Away.GetShootoutScore() : null;
                HomeIsWithdrawn = item.Home.IsWithdrawn;
                AwayIsWithdrawn = item.Away.IsWithdrawn;
                AfterExtraTime = item.AfterExtraTime;
                State = item.State;
                if (item.OriginDate == item.Date)
                    PostponedDateTime.Clear();
                else
                    PostponedDateTime.Load(item.Date);
                PostponedState = !PostponedDateTime.HasValue && item.State != MatchState.Postponed ? PostponedState.None : PostponedDateTime.HasValue ? PostponedState.SpecifiedDate : PostponedState.UnknownDate;
                ScheduleAutomatic = false;
                ScheduleStadiumAutomatic = false;
            }
        }
    }
}
