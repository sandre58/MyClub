// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
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
using MyClub.Scorer.Wpf.ViewModels.SchedulingAssistant;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections;
using MyNet.Observable.Threading;
using MyNet.Observable.Translatables;
using MyNet.UI.Commands;
using MyNet.UI.Dialogs;
using MyNet.UI.Resources;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using PropertyChanged;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class MatchEditionViewModel : EntityEditionViewModel<Match, MatchDto, MatchService>
    {
        private readonly MatchesProvider _matchesProvider;
        private readonly AvailibilityCheckingService _availibilityCheckingService;
        private readonly ReadOnlyObservableCollection<IMatchParent> _parents;
        private readonly ThreadSafeObservableCollection<TeamViewModel> _availableTeams = [];

        public MatchEditionViewModel(MatchService matchService,
                                     AvailibilityCheckingService availibilityCheckingService,
                                     StadiumsProvider stadiumsProvider,
                                     MatchesProvider matchesProvider,
                                     MatchdaysProvider matchdaysProvider) : base(matchService)
        {
            _matchesProvider = matchesProvider;
            _availibilityCheckingService = availibilityCheckingService;
            StadiumSelection = new WrapperListViewModel<StadiumViewModel, StadiumWrapper>(stadiumsProvider.Connect(), x => new StadiumWrapper(x));
            AvailableTeams = new(_availableTeams);

            Mode = ScreenMode.Edition;

            SearchAvailableDateCommand = CommandsManager.Create(async () => await ScheduleDateAsync().ConfigureAwait(false));
            SearchAvailablePostponedDateCommand = CommandsManager.Create(async () => await SchedulePostponedDateAsync().ConfigureAwait(false));

            ValidationRules.Add<MatchEditionViewModel, TeamViewModel?>(x => x.HomeTeam, MessageResources.FieldXMustBeDifferentOfFieldYError.FormatWith(MyClubResources.HomeTeam, MyClubResources.AwayTeam), x => x is null || AwayTeam is null || x.Id != AwayTeam.Id);

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
                    _availableTeams.Set(x.Value?.GetAvailableTeams() ?? []);

                    if(IsModifiedSuspender.IsSuspended) return;

                    Reset();
                }),
                this.WhenPropertyChanged(x => x.HomeTeam, false).Subscribe(x =>
                {
                    if (!IsModifiedSuspender.IsSuspended && Mode == ScreenMode.Creation && !NeutralVenue)
                        StadiumSelection.SelectedItem = x.Value?.Stadium;

                    ValidateOriginDateAvaibility();
                    ValidatePostponedDateAvaibility();
                }),
                this.WhenPropertyChanged(x => x.AwayTeam, false).Subscribe(_ =>
                {
                    ValidateOriginDateAvaibility();
                    ValidatePostponedDateAvaibility();
                }),
                StadiumSelection.WhenPropertyChanged(x => x.SelectedItem, false).Subscribe(_ => ValidateStadiumsAvaibility()),
                this.WhenAnyPropertyChanged(nameof(Date), nameof(Time)).Subscribe(x =>
                {
                    ValidateOriginDateAvaibility();
                    ValidateStadiumsAvaibility();
                }),
                this.WhenAnyPropertyChanged(nameof(PostponedDate), nameof(PostponedTime)).Subscribe(x =>
                {
                    ValidatePostponedDateAvaibility();
                    ValidateStadiumsAvaibility();
                }),
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

        [IsRequired]
        [Display(Name = nameof(Date), ResourceType = typeof(MyClubResources))]
        public DateTime? Date { get; set; }

        [IsRequired]
        [Display(Name = nameof(Time), ResourceType = typeof(MyClubResources))]
        public TimeSpan? Time { get; set; }


        [Display(Name = nameof(PostponedDate), ResourceType = typeof(MyClubResources))]
        public DateTime? PostponedDate { get; set; }

        [Display(Name = nameof(PostponedTime), ResourceType = typeof(MyClubResources))]
        public TimeSpan? PostponedTime { get; set; }

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
        public TeamViewModel? HomeTeam { get; set; }

        [IsRequired]
        [Display(Name = nameof(AwayTeam), ResourceType = typeof(MyClubResources))]
        public TeamViewModel? AwayTeam { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public ReadOnlyObservableCollection<TeamViewModel> AvailableTeams { get; }

        [CanBeValidated]
        [CanSetIsModified]
        public WrapperListViewModel<StadiumViewModel, StadiumWrapper> StadiumSelection { get; }

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

        public ICommand SearchAvailableDateCommand { get; }

        public ICommand SearchAvailablePostponedDateCommand { get; }

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

            if (!Date.HasValue)
            {
                DateAvailability = AvailabilityCheck.Unknown;
                return;
            }
            var date = Date.Value;

            date = date.AddFluentTimeSpan(Time ?? DateTime.Today.EndOfDay().TimeOfDay);
            DateAvailability = CheckTeamsAvaibility(date);
        }

        private void ValidatePostponedDateAvaibility()
        {
            if (IsModifiedSuspender.IsSuspended) return;

            if (!PostponedDate.HasValue)
            {
                PostponedDateAvailability = AvailabilityCheck.Unknown;
                return;
            }
            var date = PostponedDate.Value;

            date = date.AddFluentTimeSpan(PostponedTime ?? DateTime.Today.EndOfDay().TimeOfDay);
            PostponedDateAvailability = CheckTeamsAvaibility(date);
        }

        private void ValidateStadiumsAvaibility()
        {
            if (IsModifiedSuspender.IsSuspended) return;

            DateTime? date = null;

            if (PostponedState == PostponedState.SpecifiedDate && PostponedDate.HasValue)
                date = PostponedDate.Value.AddFluentTimeSpan(PostponedTime ?? DateTime.Today.EndOfDay().TimeOfDay);
            else if (Date.HasValue)
                date = Date.Value.AddFluentTimeSpan(Time ?? DateTime.Today.EndOfDay().TimeOfDay);

            foreach (var item in StadiumSelection.Wrappers)
                item.Availability = date.HasValue ? CheckStadiumAvaibility(item.Item.Id, date.Value) : AvailabilityCheck.Unknown;
        }

        private AvailabilityCheck CheckTeamsAvaibility(DateTime date)
            => HomeTeam is null && AwayTeam is null
                ? AvailabilityCheck.Unknown
                : _availibilityCheckingService.GetTeamsAvaibility(new[] { HomeTeam?.Id, AwayTeam?.Id }.NotNull().OfType<Guid>(), date, MatchFormat.Create(), [ItemId ?? Guid.Empty]);

        private AvailabilityCheck CheckStadiumAvaibility(Guid stadiumId, DateTime date)
            => _availibilityCheckingService.GetStadiumAvaibility(stadiumId, date, MatchFormat.Create(), [ItemId ?? Guid.Empty]);

        private async Task ScheduleDateAsync()
        {
            var date = await SearchAvailableDateAsync(Date?.AddFluentTimeSpan(Time ?? TimeSpan.Zero)).ConfigureAwait(false);

            if (date.HasValue)
            {
                Date = date.Value.Date;
                Time = date.Value.TimeOfDay;
            }
        }

        private async Task SchedulePostponedDateAsync()
        {
            var date = await SearchAvailableDateAsync(PostponedDate?.AddFluentTimeSpan(PostponedTime ?? TimeSpan.Zero)).ConfigureAwait(false);

            if (date.HasValue)
            {
                PostponedDate = date.Value.Date;
                PostponedTime = date.Value.TimeOfDay;
            }
        }

        private async Task<DateTime?> SearchAvailableDateAsync(DateTime? date)
        {
            var series = new List<SchedulingSerie>();

            if (HomeTeam is not null)
                series.Add(new SchedulingSerie(_matchesProvider.Items.Where(x => x.Participate(HomeTeam)).Where(x => x.Id != ItemId), HomeTeam, HomeTeam.HomeColor.GetValueOrDefault()));
            if (AwayTeam is not null)
                series.Add(new SchedulingSerie(_matchesProvider.Items.Where(x => x.Participate(AwayTeam)).Where(x => x.Id != ItemId), AwayTeam, AwayTeam.HomeColor.GetValueOrDefault()));
            if (StadiumSelection.SelectedItem is not null)
                series.Add(new SchedulingSerie(_matchesProvider.Items.Where(x => x.Stadium is not null && x.Stadium == StadiumSelection.SelectedItem).Where(x => x.Id != ItemId), StadiumSelection.SelectedItem));

            var vm = new SchedulingAssistantViewModel(series, date);

            var result = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);

            return result.IsTrue() && vm.FullDate.HasValue ? vm.FullDate?.ToLocalTime() : null;
        }

        public void Load(MatchViewModel match)
        {
            Parent = match.Parent;
            Load(match.Id);
        }

        public void New(IMatchParent? parent = null, Action? initialize = null)
        {
            Parent = parent ?? _parents.LastOrDefault();
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
                MatchFormat.Load(defaultValues.Format ?? Scorer.Domain.MatchAggregate.MatchFormat.Default);
                Date = defaultValues.Date.Date;
                Time = defaultValues.Date.TimeOfDay;
                HomeTeam = null;
                AwayTeam = null;
                StadiumSelection.SelectedItem = defaultValues.Stadium?.Id is not null ? StadiumSelection.Items.GetByIdOrDefault(defaultValues.Stadium.Id.Value) : null;
                NeutralVenue = defaultValues.NeutralVenue;
                HomeScore.Value = defaultValues.HomeScore;
                AwayScore.Value = defaultValues.AwayScore;
                HomeShootoutScore.Value = defaultValues.HomeShootoutScore;
                AwayShootoutScore.Value = defaultValues.AwayShootoutScore;
                HomeIsWithdrawn = defaultValues.HomeIsWithdrawn;
                AwayIsWithdrawn = defaultValues.AwayIsWithdrawn;
                AfterExtraTime = defaultValues.AfterExtraTime;
                State = defaultValues.State;
                PostponedState = PostponedState.None;
                PostponedDate = null;
                PostponedTime = null;
            }
        }

        protected override MatchDto ToDto()
            => new()
            {
                Id = ItemId,
                ParentId = Parent?.Id ?? throw new InvalidOperationException("Parent cannot be null"),
                HomeTeamId = HomeTeam?.Id ?? throw new InvalidOperationException("HomeTeam cannot be null"),
                AwayTeamId = AwayTeam?.Id ?? throw new InvalidOperationException("AwayTeam cannot be null"),
                Date = Date.GetValueOrDefault().ToUtcDateTime(Time.GetValueOrDefault(Parent.MatchTime)),
                Format = CanEditFormat && MatchFormat.IsModified() ? MatchFormat.Create() : null,
                NeutralVenue = NeutralVenue,
                Stadium = StadiumSelection.SelectedItem is not null ? new StadiumDto
                {
                    Id = StadiumSelection.SelectedItem.Id,
                    Name = StadiumSelection.SelectedItem.Name,
                    Ground = StadiumSelection.SelectedItem.Ground,
                    Address = StadiumSelection.SelectedItem.Address,
                } : null,
                HomeScore = HomeScore.Value,
                AwayScore = AwayScore.Value,
                HomeShootoutScore = HomeShootoutScore.Value,
                AwayShootoutScore = AwayShootoutScore.Value,
                HomeIsWithdrawn = HomeIsWithdrawn,
                AwayIsWithdrawn = AwayIsWithdrawn,
                AfterExtraTime = AfterExtraTime,
                State = State,
                PostponedDate = PostponedState == PostponedState.SpecifiedDate ? PostponedDate?.Date.ToUtcDateTime(PostponedTime.GetValueOrDefault(Parent.MatchTime)) : null
            };

        protected override void RefreshFrom(Match item)
        {
            if (Parent is not null)
            {
                HomeTeam = Parent.GetAvailableTeams().GetById(item.HomeTeam.Id);
                AwayTeam = Parent.GetAvailableTeams().GetById(item.AwayTeam.Id);
                StadiumSelection.SelectedItem = item.Stadium?.Id is not null ? StadiumSelection.Items.GetByIdOrDefault(item.Stadium.Id) : null;
                Date = item.OriginDate.Date;
                Time = item.OriginDate.ToLocalTime().TimeOfDay;
                MatchFormat.Load(item.Format);
                NeutralVenue = item.NeutralVenue;
                HomeScore.Value = item.State == MatchState.InProgress || item.State == MatchState.Suspended || item.State == MatchState.Played ? item.Home.GetScore() : null;
                AwayScore.Value = item.State == MatchState.InProgress || item.State == MatchState.Suspended || item.State == MatchState.Played ? item.Away.GetScore() : null;
                HomeShootoutScore.Value = item.State == MatchState.InProgress || item.State == MatchState.Suspended || item.State == MatchState.Played ? item.Home.GetShootoutScore() : null;
                AwayShootoutScore.Value = item.State == MatchState.InProgress || item.State == MatchState.Suspended || item.State == MatchState.Played ? item.Away.GetShootoutScore() : null;
                HomeIsWithdrawn = item.Home.IsWithdrawn;
                AwayIsWithdrawn = item.Away.IsWithdrawn;
                AfterExtraTime = item.AfterExtraTime;
                State = item.State;
                PostponedDate = item.OriginDate == item.Date ? null : item.Date.Date;
                PostponedTime = item.OriginDate == item.Date ? null : item.Date.ToLocalTime().TimeOfDay;
                PostponedState = !PostponedDate.HasValue && item.State != MatchState.Postponed ? PostponedState.None : PostponedDate.HasValue ? PostponedState.SpecifiedDate : PostponedState.UnknownDate;
            }
        }
    }

    internal class StadiumWrapper : Wrapper<StadiumViewModel>
    {
        public AvailabilityCheck Availability { get; set; }

        public StadiumWrapper(StadiumViewModel item) : base(item) { }
    }
}
