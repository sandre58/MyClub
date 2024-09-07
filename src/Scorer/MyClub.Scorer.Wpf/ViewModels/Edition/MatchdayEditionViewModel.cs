// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Commands;
using MyNet.UI.Threading;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    public enum PostponedState
    {
        None,

        UnknownDate,

        SpecifiedDate
    }

    internal class MatchdayEditionViewModel : EntityEditionViewModel<Matchday, MatchdayDto, MatchdayService>
    {
        private readonly ReadOnlyObservableCollection<MatchdayViewModel> _matchdays;
        private readonly ISourceProvider<ITeamViewModel> _teamsProvider;
        private readonly ISourceProvider<IStadiumViewModel> _stadiumsProvider;

        public MatchdayEditionViewModel(MatchdayService matchdayService,
                                        MatchdaysProvider matchdaysProvider,
                                        StadiumsProvider stadiumsProvider,
                                        TeamsProvider teamsProvider) : base(matchdayService)
        {
            _stadiumsProvider = stadiumsProvider;
            _teamsProvider = teamsProvider;
            InvertTeamsCommand = CommandsManager.Create(InvertTeams, () => Matches.Any(x => !x.IsReadOnly));
            AddMatchCommand = CommandsManager.Create(AddMatch, () => teamsProvider.Items.Count > 0);
            RemoveMatchCommand = CommandsManager.CreateNotNull<EditableMatchViewModel>(RemoveMatch);
            CancelRemoveMatchCommand = CommandsManager.CreateNotNull<EditableMatchViewModel>(CancelRemoveMatch);
            DuplicateMatchdayCommand = CommandsManager.CreateNotNull<MatchdayViewModel>(DuplicateMatchday);
            ClearDuplicatedMatchdayCommand = CommandsManager.Create(ClearDuplicatedMatchday);

            Disposables.AddRange(
            [
                matchdaysProvider.ConnectById().SortBy(x => x.Date).Bind(out _matchdays).ObserveOn(Scheduler.UI).Subscribe(),
            ]);
        }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public IMatchdayParent? Parent { get; private set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public ReadOnlyObservableCollection<MatchdayViewModel> Matchdays => _matchdays;

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public MatchdayViewModel? DuplicatedMatchday { get; set; }

        [IsRequired]
        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        public virtual string Name { get; set; } = string.Empty;

        [IsRequired]
        [Display(Name = nameof(ShortName), ResourceType = typeof(MyClubResources))]

        public virtual string ShortName { get; set; } = string.Empty;

        public EditableDateTime CurrentDate { get; set; } = new();

        public EditableDateTime PostponedDateTime { get; set; } = new(false);

        public PostponedState PostponedState { get; set; }

        public bool ScheduleAutomatic { get; set; }

        public bool ScheduleStadiumsAutomatic { get; set; }

        public bool CanScheduleAutomatic { get; set; }

        public bool CanScheduleStadiumsAutomatic { get; set; }

        public ObservableCollection<EditableMatchViewModel> Matches { get; } = [];

        public ICommand AddMatchCommand { get; }

        public ICommand RemoveMatchCommand { get; }

        public ICommand CancelRemoveMatchCommand { get; }

        public ICommand InvertTeamsCommand { get; }

        public ICommand DuplicateMatchdayCommand { get; }

        public ICommand ClearDuplicatedMatchdayCommand { get; }

        private void RemoveMatch(EditableMatchViewModel item)
        {
            if (item.IsReadOnly)
                item.IsDeleting = true;
            else
                Matches.Remove(item);
        }

        private void CancelRemoveMatch(EditableMatchViewModel item) => item.IsDeleting = false;

        private void AddMatch()
        {
            var item = new EditableMatchViewModel(_teamsProvider, _stadiumsProvider, (Parent?.SchedulingParameters.UseHomeVenue).IsTrue());

            item.CurrentDate.Date = CurrentDate.Date;
            item.CurrentDate.Time = CurrentDate.Time;
            Matches.Add(item);
        }

        private void InvertTeams() => Matches.Where(x => !x.IsReadOnly).ForEach(x => x.InvertTeams());

        private void DuplicateMatchday(MatchdayViewModel matchday)
        {
            DuplicatedMatchday = matchday;
            Matches.Set(matchday.Matches.OrderBy(x => x.Date).Select(x =>
            {
                var result = new EditableMatchViewModel(_teamsProvider, _stadiumsProvider, (Parent?.SchedulingParameters.UseHomeVenue).IsTrue());

                if (CurrentDate.HasValue)
                    result.CurrentDate.Load(CurrentDate.DateTime.GetValueOrDefault());
                result.HomeTeam = result.AvailableTeams.GetById(x.HomeTeam.Id);
                result.AwayTeam = result.AvailableTeams.GetById(x.AwayTeam.Id);
                result.StadiumSelection.Select(x.Stadium?.Id);

                return result;
            }));
        }

        private void ClearDuplicatedMatchday()
        {
            DuplicatedMatchday = null;
            Matches.Clear();
        }

        public void New(IMatchdayParent parent, DateTime? date = null)
        {
            DuplicatedMatchday = null;
            Parent = parent;
            CanScheduleAutomatic = Parent?.CanAutomaticReschedule() ?? false;
            CanScheduleStadiumsAutomatic = Parent?.CanAutomaticRescheduleVenue() ?? false;
            New(() =>
            {
                if (date.HasValue)
                {
                    if (date.Value.TimeOfDay != TimeSpan.Zero)
                        CurrentDate.Load(date.Value);
                    else
                        CurrentDate.Load(date.Value.At(CurrentDate.Time.GetValueOrDefault()));
                }
            });
        }

        public void Duplicate(MatchdayViewModel matchday, DateTime? date = null)
        {
            New(matchday.Parent, date);
            DuplicatedMatchday = matchday;
        }

        public void Load(MatchdayViewModel matchday)
        {
            DuplicatedMatchday = null;
            Parent = matchday.Parent;
            CanScheduleAutomatic = Parent?.CanAutomaticReschedule() ?? false;
            CanScheduleStadiumsAutomatic = Parent?.CanAutomaticRescheduleVenue() ?? false;
            Load(matchday.Id);
        }

        protected override void ResetItem()
        {
            var defaultValues = CrudService.New(Parent?.Id);
            CurrentDate.Load(defaultValues.Date);
            ShortName = defaultValues.ShortName.OrEmpty();
            Name = defaultValues.Name.OrEmpty();
            PostponedState = PostponedState.None;
            PostponedDateTime.Clear();
            ScheduleAutomatic = false;
            ScheduleStadiumsAutomatic = false;

            if (DuplicatedMatchday is not null)
                DuplicateMatchday(DuplicatedMatchday);
            else
                Matches.Clear();
        }

        protected override MatchdayDto ToDto()
            => new()
            {
                Id = ItemId,
                ParentId = Parent?.Id,
                Name = Name,
                ShortName = ShortName,
                Date = CurrentDate.ToUtcOrDefault(),
                IsPostponed = PostponedState == PostponedState.UnknownDate,
                PostponedDate = PostponedState == PostponedState.SpecifiedDate ? PostponedDateTime.ToUtc() : null,
                MatchesToDelete = Matches.Where(x => x.IsDeleting && x.Id.HasValue).Select(x => x.Id!.Value).ToList(),
                MatchesToAdd = Matches.Where(x => !x.Id.HasValue && x.IsValid()).Select(x => new MatchDto
                {
                    AwayTeamId = x.AwayTeam!.Id,
                    HomeTeamId = x.HomeTeam!.Id,
                    Date = x.CurrentDate.ToUtcOrDefault(),
                    Stadium = x.StadiumSelection.SelectedItem is not null ? new StadiumDto
                    {
                        Id = x.StadiumSelection.SelectedItem.Id,
                        Name = x.StadiumSelection.SelectedItem.Name,
                        Ground = x.StadiumSelection.SelectedItem.Ground,
                        Address = x.StadiumSelection.SelectedItem.Address,
                    } : null,
                    State = MatchState.None
                }).ToList(),
                ScheduleStadiumsAutomatic = CanScheduleStadiumsAutomatic && ScheduleStadiumsAutomatic,
                ScheduleAutomatic = CanScheduleAutomatic && ScheduleAutomatic
            };

        protected override void RefreshFrom(Matchday item)
        {
            Name = item.Name;
            ShortName = item.ShortName;
            CurrentDate.Load(item.OriginDate);
            if (item.OriginDate == item.Date)
                PostponedDateTime.Clear();
            else
                PostponedDateTime.Load(item.Date);
            PostponedState = !PostponedDateTime.HasValue && !item.IsPostponed ? PostponedState.None : PostponedDateTime.HasValue ? PostponedState.SpecifiedDate : PostponedState.UnknownDate;
            Matches.Set(item.Matches.OrderBy(x => x.Date).Select(x =>
            {
                var result = new EditableMatchViewModel(x.Id, _teamsProvider, _stadiumsProvider, (Parent?.SchedulingParameters.UseHomeVenue).IsTrue());
                result.CurrentDate.Load(x.Date);
                result.HomeTeam = result.AvailableTeams.GetById(x.HomeTeam.Id);
                result.AwayTeam = result.AvailableTeams.GetById(x.AwayTeam.Id);
                result.StadiumSelection.Select(x.Stadium?.Id);

                return result;
            }));
            ScheduleAutomatic = false;
            ScheduleStadiumsAutomatic = false;
        }
    }
}
