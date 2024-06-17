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
using MyClub.Scorer.Wpf.Messages;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Commands;
using MyNet.UI.Threading;
using MyNet.Utilities;
using MyNet.Utilities.Messaging;

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
        private readonly ISourceProvider<TeamViewModel> _teamsProvider;
        private readonly ISourceProvider<StadiumViewModel> _stadiumsProvider;

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
        public Guid? ParentId { get; private set; }

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

        [IsRequired]
        [Display(Name = nameof(Date), ResourceType = typeof(MyClubResources))]
        public virtual DateTime? Date { get; set; }

        [IsRequired]
        [Display(Name = nameof(Time), ResourceType = typeof(MyClubResources))]
        public TimeSpan? Time { get; set; }

        [Display(Name = nameof(PostponedDate), ResourceType = typeof(MyClubResources))]
        public DateTime? PostponedDate { get; set; }

        [Display(Name = nameof(PostponedTime), ResourceType = typeof(MyClubResources))]
        public TimeSpan? PostponedTime { get; set; }

        public PostponedState PostponedState { get; set; }

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

        private void AddMatch() => Matches.Add(new EditableMatchViewModel(_teamsProvider, _stadiumsProvider)
        {
            Date = Date,
            Time = Time,
        });

        private void InvertTeams() => Matches.Where(x => !x.IsReadOnly).ForEach(x => x.InvertTeams());

        private void DuplicateMatchday(MatchdayViewModel matchday)
        {
            DuplicatedMatchday = matchday;
            Matches.Set(matchday.Matches.OrderBy(x => x.Date).Select(x =>
            {
                var result = new EditableMatchViewModel(_teamsProvider, _stadiumsProvider)
                {
                    Date = Date,
                    Time = Time,
                };
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

        public void New(Guid? parentId = null, Action? initialize = null)
        {
            DuplicatedMatchday = null;
            ParentId = parentId;
            New(initialize);
        }

        public void Duplicate(MatchdayViewModel matchday, Action? initialize = null)
        {
            New(matchday.Parent.Id, initialize);
            DuplicatedMatchday = matchday;
        }

        public void Load(MatchdayViewModel matchday)
        {
            DuplicatedMatchday = null;
            ParentId = matchday.Parent.Id;
            Load(matchday.Id);
        }

        protected override void ResetItem()
        {
            var defaultValues = CrudService.New(ParentId);
            Date = defaultValues.Date.ToLocalTime().Date;
            Time = defaultValues.Date.ToLocalTime().TimeOfDay;
            ShortName = defaultValues.ShortName.OrEmpty();
            Name = defaultValues.Name.OrEmpty();
            PostponedState = PostponedState.None;
            PostponedDate = null;

            if (DuplicatedMatchday is not null)
                DuplicateMatchday(DuplicatedMatchday);
            else
                Matches.Clear();
        }

        protected override MatchdayDto ToDto()
            => new()
            {
                Id = ItemId,
                ParentId = ParentId,
                Name = Name,
                ShortName = ShortName,
                Date = Date.GetValueOrDefault().ToUtcDateTime(Time.GetValueOrDefault()),
                IsPostponed = PostponedState == PostponedState.UnknownDate,
                PostponedDate = PostponedState == PostponedState.SpecifiedDate ? PostponedDate?.Date.ToUtcDateTime(PostponedTime ?? DateTime.Now.TimeOfDay) : null,
                MatchesToDelete = Matches.Where(x => x.IsDeleting && x.Id.HasValue).Select(x => x.Id!.Value).ToList(),
                MatchesToAdd = Matches.Where(x => !x.Id.HasValue && x.IsValid()).Select(x => new MatchDto
                {
                    AwayTeamId = x.AwayTeam!.Id,
                    HomeTeamId = x.HomeTeam!.Id,
                    Date = x.Date!.Value.ToUtcDateTime(x.Time!.Value),
                    Stadium = x.StadiumSelection.SelectedItem is not null ? new StadiumDto
                    {
                        Id = x.StadiumSelection.SelectedItem.Id,
                        Name = x.StadiumSelection.SelectedItem.Name,
                        Ground = x.StadiumSelection.SelectedItem.Ground,
                        Address = x.StadiumSelection.SelectedItem.Address,
                    } : null,
                    State = MatchState.None
                }).ToList()
            };

        protected override void RefreshFrom(Matchday item)
        {
            Name = item.Name;
            ShortName = item.ShortName;
            Date = item.OriginDate.Date;
            Time = item.OriginDate.ToLocalTime().TimeOfDay;
            PostponedDate = item.OriginDate == item.Date ? null : item.Date.Date;
            PostponedTime = item.OriginDate == item.Date ? null : item.Date.ToLocalTime().TimeOfDay;
            PostponedState = !PostponedDate.HasValue && !item.IsPostponed ? PostponedState.None : PostponedDate.HasValue ? PostponedState.SpecifiedDate : PostponedState.UnknownDate;
            Matches.Set(item.Matches.OrderBy(x => x.Date).Select(x =>
            {
                var result = new EditableMatchViewModel(x.Id, _teamsProvider, _stadiumsProvider)
                {
                    Date = x.Date.ToLocalTime().Date,
                    Time = x.Date.ToLocalTime().TimeOfDay,
                };
                result.HomeTeam = result.AvailableTeams.GetById(x.HomeTeam.Id);
                result.AwayTeam = result.AvailableTeams.GetById(x.AwayTeam.Id);
                result.StadiumSelection.Select(x.Stadium?.Id);

                return result;
            }));
        }

        protected override void SaveCore()
        {
            base.SaveCore();
            Messenger.Default.Send(new CheckConflictsRequestMessage());
        }
    }
}
