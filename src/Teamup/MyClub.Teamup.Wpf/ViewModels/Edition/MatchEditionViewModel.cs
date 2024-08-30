// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using DynamicData.Binding;
using MyNet.UI.ViewModels;
using MyNet.Utilities;
using MyNet.Observable.Attributes;
using MyNet.Observable.Translatables;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Wpf.Extensions;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;
using PropertyChanged;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class MatchEditionViewModel : EntityEditionViewModel<Match, MatchDto, MatchService>
    {
        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public string? SubTitle => Equals(Parent, Competition) ? null : Parent?.Name;

        public IMatchParent? Parent { get; private set; }

        public CompetitionViewModel? Competition { get; private set; }

        [IsRequired]
        [Display(Name = nameof(Date), ResourceType = typeof(MyClubResources))]
        public DateTime? Date { get; set; }

        [IsRequired]
        [Display(Name = nameof(Time), ResourceType = typeof(MyClubResources))]
        public TimeSpan Time { get; set; }

        [Display(Name = nameof(PostponedDate), ResourceType = typeof(MyClubResources))]
        public DateTime? PostponedDate { get; set; }

        [Display(Name = nameof(PostponedTime), ResourceType = typeof(MyClubResources))]
        public TimeSpan? PostponedTime { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool ShowPostponedDate { get; set; }

        [IsRequired]
        [Display(Name = nameof(State), ResourceType = typeof(MyClubResources))]
        public MatchState State { get; set; }

        public EditableStadiumSelectionViewModel StadiumSelection { get; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public ReadOnlyObservableCollection<TeamViewModel>? AvailableTeams { get; private set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool NewStadiumWillBeCreated => StadiumSelection.SelectedItem == null && !string.IsNullOrEmpty(StadiumSelection.TextSearch);

        [IsRequired]
        [Display(Name = nameof(NeutralVenue), ResourceType = typeof(MyClubResources))]
        public bool NeutralVenue { get; set; }

        public EditableMatchFormatViewModel MatchFormat { get; } = new();

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool CanEditFormat { get; private set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool CanEditPenaltyPoints { get; private set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool CanEditExtraTimeOrShootout => (MatchFormat.ExtraTimeIsEnabled || MatchFormat.ShootoutsIsEnabled) && HasDraw;

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool ShowDetails { get; set; }

        [IsRequired]
        [Display(Name = nameof(HomeTeam), ResourceType = typeof(MyClubResources))]
        public TeamViewModel? HomeTeam { get; set; }

        [IsRequired]
        [Display(Name = nameof(HomeTeam), ResourceType = typeof(MyClubResources))]
        public TeamViewModel? AwayTeam { get; set; }

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

        [Display(Name = nameof(HomePenaltyPoints), ResourceType = typeof(MyClubResources))]
        public int HomePenaltyPoints { get; set; }

        [Display(Name = nameof(AwayPenaltyPoints), ResourceType = typeof(MyClubResources))]
        public int AwayPenaltyPoints { get; set; }

        public bool HasDraw => HomeScore == AwayScore && !HomeIsWithdrawn && !AwayIsWithdrawn;

        public MatchEditionViewModel(MatchService matchService,
                                     StadiumPresentationService stadiumPresentationService,
                                     StadiumsProvider stadiumsProvider) : base(matchService)
        {
            StadiumSelection = new EditableStadiumSelectionViewModel(stadiumsProvider, stadiumPresentationService);
            Mode = ScreenMode.Edition;

            Disposables.AddRange(
            [
                StadiumSelection.WhenAnyPropertyChanged(nameof(EditableStadiumSelectionViewModel.SelectedItem), nameof(EditableStadiumSelectionViewModel.TextSearch)).Subscribe(_ => RaisePropertyChanged(nameof(NewStadiumWillBeCreated))),
                HomeScore.WhenPropertyChanged(x => x.Value, false).Subscribe(_ => OnScoreChanged()),
                AwayScore.WhenPropertyChanged(x => x.Value, false).Subscribe(_ => OnScoreChanged()),
                this.WhenPropertyChanged(x => x.HomeIsWithdrawn, false).Subscribe(_ =>
                {
                    if(IsModifiedSuspender.IsSuspended) return;

                    if(HomeIsWithdrawn) {
                        AwayIsWithdrawn = false;
                        HomeScore.Value = 0;
                        AwayScore.Value = 3;
                        State = MatchState.Played;

                        if(CanEditPenaltyPoints)
                        {
                            HomePenaltyPoints = 1;
                            AwayPenaltyPoints = 0;
                        }
                    }
                    else
                    {
                        HomePenaltyPoints = 0;
                    }
                }),
                this.WhenPropertyChanged(x => x.AwayIsWithdrawn, false).Subscribe(_ =>
                {
                    if(IsModifiedSuspender.IsSuspended) return;

                    if(AwayIsWithdrawn) {
                        HomeIsWithdrawn = false;
                        HomeScore.Value = 3;
                        AwayScore.Value = 0;
                        State = MatchState.Played;

                        if(CanEditPenaltyPoints)
                        {
                            HomePenaltyPoints = 0;
                            AwayPenaltyPoints = 1;
                        }
                    }
                    else
                    {
                        AwayPenaltyPoints = 0;
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
                }),
                this.WhenPropertyChanged(x => x.HomeTeam, false).Subscribe(x =>
                {
                    if (!IsModifiedSuspender.IsSuspended && Mode == ScreenMode.Creation && !NeutralVenue)
                        StadiumSelection.Select(x.Value?.Stadium?.Id);
                })
            ]);
        }

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

        protected override string CreateTitle()
        {
            RaisePropertyChanged(nameof(SubTitle));
            return Competition?.Name ?? string.Empty;
        }

        public void Load(IMatchParent parent, Guid matchId)
        {
            Parent = parent;
            Competition = parent.GetCompetition();
            CanEditFormat = parent.CanEditMatchFormat();
            CanEditPenaltyPoints = parent.CanEditPenaltyPoints();
            AvailableTeams = new ReadOnlyObservableCollection<TeamViewModel>(parent.GetAvailableTeams().OrderBy(x => x.Name).ToObservableCollection());
            Load(matchId);
            UpdateTitle();
        }

        public void New(IMatchParent parent, Action? initialize = null)
        {
            Parent = parent;
            Competition = parent.GetCompetition();
            CanEditFormat = parent.CanEditMatchFormat();
            CanEditPenaltyPoints = parent.CanEditPenaltyPoints();
            AvailableTeams = new ReadOnlyObservableCollection<TeamViewModel>(parent.GetAvailableTeams().OrderBy(x => x.Name).ToObservableCollection());
            New(initialize);
            UpdateTitle();
        }

        protected override void ResetItem()
        {
            if (Parent is not null)
            {
                var defaultValues = CrudService.New(Parent.Id, Parent.GetDefaultDateTime());
                HomeTeam = null;
                AwayTeam = null;
                Date = defaultValues.Date.Date;
                Time = defaultValues.Date.TimeOfDay;
                MatchFormat.Load(defaultValues.Format ?? Parent.Rules.MatchFormat);
                StadiumSelection.Select(defaultValues.Stadium?.Id);
                NeutralVenue = defaultValues.NeutralVenue;
                HomeScore.Value = defaultValues.HomeScore;
                AwayScore.Value = defaultValues.AwayScore;
                HomeShootoutScore.Value = defaultValues.HomeShootoutScore;
                AwayShootoutScore.Value = defaultValues.AwayShootoutScore;
                HomeIsWithdrawn = defaultValues.HomeIsWithdrawn;
                AwayIsWithdrawn = defaultValues.AwayIsWithdrawn;
                AfterExtraTime = defaultValues.AfterExtraTime;
                HomePenaltyPoints = defaultValues.HomePenaltyPoints;
                AwayPenaltyPoints = defaultValues.AwayPenaltyPoints;
                State = defaultValues.State;
                PostponedDate = defaultValues.PostponedDate?.Date;
                PostponedTime = defaultValues.PostponedDate?.TimeOfDay;
                ShowPostponedDate = PostponedDate.HasValue;
            }
        }

        protected override MatchDto ToDto()
            => new()
            {
                Id = ItemId,
                ParentId = Parent?.Id,
                HomeTeamId = HomeTeam?.Id,
                AwayTeamId = AwayTeam?.Id,
                Date = Date.GetValueOrDefault().ToUtc(Time),
                Format = CanEditFormat && MatchFormat.IsModified() ? MatchFormat.Create() : null,
                NeutralVenue = NeutralVenue,
                Stadium = StadiumSelection.SelectedItem is not null || !string.IsNullOrEmpty(StadiumSelection.TextSearch) ? new StadiumDto
                {
                    Id = StadiumSelection.SelectedItem?.Id ?? Guid.NewGuid(),
                    Name = StadiumSelection.SelectedItem?.Name ?? StadiumSelection.TextSearch,
                    Ground = StadiumSelection.SelectedItem?.Ground ?? Ground.Grass,
                    Address = StadiumSelection.SelectedItem?.Address,
                } : null,
                HomeScore = HomeScore.Value,
                AwayScore = AwayScore.Value,
                HomeShootoutScore = HomeShootoutScore.Value,
                AwayShootoutScore = AwayShootoutScore.Value,
                HomeIsWithdrawn = HomeIsWithdrawn,
                AwayIsWithdrawn = AwayIsWithdrawn,
                AfterExtraTime = AfterExtraTime,
                HomePenaltyPoints = HomePenaltyPoints,
                AwayPenaltyPoints = AwayPenaltyPoints,
                State = State,
                PostponedDate = ShowPostponedDate ? PostponedDate?.ToUtc(PostponedTime ?? Parent?.GetDefaultDateTime().TimeOfDay ?? DateTime.Now.TimeOfDay) : null,
            };

        protected override void RefreshFrom(Match item)
        {
            if (Parent is not null)
            {
                HomeTeam = Parent.GetAvailableTeams().GetById(item.HomeTeam.Id);
                AwayTeam = Parent.GetAvailableTeams().GetById(item.AwayTeam.Id);
                Date = item.OriginDate.Date;
                Time = item.OriginDate.ToLocalTime().TimeOfDay;
                MatchFormat.Load(item.Format);
                StadiumSelection.Select(item.Stadium?.Id);
                NeutralVenue = item.NeutralVenue;
                HomeScore.Value = item.State == MatchState.InProgress || item.State == MatchState.Suspended || item.State == MatchState.Played ? item.Home.GetScore() : null;
                AwayScore.Value = item.State == MatchState.InProgress || item.State == MatchState.Suspended || item.State == MatchState.Played ? item.Away.GetScore() : null;
                HomeShootoutScore.Value = item.State == MatchState.InProgress || item.State == MatchState.Suspended || item.State == MatchState.Played ? item.Home.GetShootoutScore() : null;
                AwayShootoutScore.Value = item.State == MatchState.InProgress || item.State == MatchState.Suspended || item.State == MatchState.Played ? item.Away.GetShootoutScore() : null;
                HomeIsWithdrawn = item.Home.IsWithdrawn;
                AwayIsWithdrawn = item.Away.IsWithdrawn;
                AfterExtraTime = item.AfterExtraTime;
                HomePenaltyPoints = item.Home.PenaltyPoints;
                AwayPenaltyPoints = item.Away.PenaltyPoints;
                State = item.State;
                PostponedDate = item.OriginDate == item.Date ? null : item.Date.Date;
                PostponedTime = item.OriginDate == item.Date ? Parent.GetDefaultDateTime().TimeOfDay : item.Date.ToLocalTime().TimeOfDay;
                ShowPostponedDate = PostponedDate.HasValue;
            }
        }
    }
}
