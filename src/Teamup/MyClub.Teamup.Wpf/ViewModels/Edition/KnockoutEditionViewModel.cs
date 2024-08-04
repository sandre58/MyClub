// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Subjects;
using DynamicData.Binding;
using MyNet.UI.Commands;
using MyNet.Utilities;
using MyNet.Observable.Attributes;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.UI.Collections;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class KnockoutEditionViewModel : EntityEditionViewModel<IRound, RoundDto, RoundService>
    {
        public KnockoutEditionViewModel(RoundService crudService) : base(crudService)
        {
            var teamsChanged = new Subject<Func<EditableTeamViewModel, bool>>();
            TeamSelectionViewModel = new(teamsChanged);

            AddSelectedTeamCommand = CommandsManager.Create(ValidateAndAddTeam, () => TeamSelectionViewModel.SelectedItem is not null || !string.IsNullOrEmpty(TeamSelectionViewModel.TextSearch));
            RemoveTeamCommand = CommandsManager.CreateNotNull<TeamViewModel>(x => Teams.Remove(x), x => x is not null);

            Disposables.AddRange(
            [
                Teams.ToObservableChangeSet().Subscribe(_ => teamsChanged.OnNext(x => !Teams.Select(y => y.Id).Contains(x.Id!.Value)))
            ]);
        }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public CupViewModel? Parent { get; private set; }

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
        public TimeSpan Time { get; set; }

        [Display(Name = nameof(PostponedDate), ResourceType = typeof(MyClubResources))]
        public DateTime? PostponedDate { get; set; }

        [Display(Name = nameof(PostponedTime), ResourceType = typeof(MyClubResources))]
        public TimeSpan? PostponedTime { get; set; }

        [IsRequired]
        [Display(Name = nameof(IsPostponed), ResourceType = typeof(MyClubResources))]
        public bool IsPostponed { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool ShowPostponedDate { get; set; }

        public TimeSpan MatchTime { get; set; }

        public EditableMatchFormatViewModel MatchFormat { get; } = new();

        [HasUniqueItems]
        [Display(Name = nameof(Teams), ResourceType = typeof(MyClubResources))]
        public UiObservableCollection<TeamViewModel> Teams { get; } = [];

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public EditableTeamSelectionViewModel TeamSelectionViewModel { get; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool ShowRemovingTeamWarning { get; set; }

        public ICommand AddSelectedTeamCommand { get; private set; }

        public ICommand RemoveTeamCommand { get; private set; }

        public void New(CupViewModel parent, Action? initialize = null)
        {
            Parent = parent;
            New(initialize);
            UpdateTitle();
        }

        public void Load(CupViewModel parent, Guid matchdayId)
        {
            Load(matchdayId);
            Parent = parent;
            UpdateTitle();
        }

        protected override string CreateTitle() => Parent?.Name ?? string.Empty;

        protected override void ResetItem()
        {
            if (Parent is not null)
            {
                var defaultValues = CrudService.NewKnockout(Parent.Id);
                Date = defaultValues.Date.Date;
                Time = defaultValues.Date.ToLocalTime().TimeOfDay;
                ShortName = defaultValues.ShortName.OrEmpty();
                Teams.Set(Parent.GetAvailableTeams().Where(x => (defaultValues.TeamIds ?? []).Contains(x.Id)).ToList());
                Name = defaultValues.Name.OrEmpty();

                if (defaultValues.Rules is not null)
                {
                    MatchTime = defaultValues.Rules.MatchTime;
                    if (defaultValues.Rules.MatchFormat is not null)
                        MatchFormat.Load(defaultValues.Rules.MatchFormat);
                    else
                        MatchFormat.Reset();
                }
                ShowRemovingTeamWarning = false;
            }
        }
        protected override RoundDto ToDto()
            => new KnockoutDto()
            {
                Id = ItemId,
                ParentId = Parent?.Id,
                Name = Name,
                ShortName = ShortName,
                Rules = new CupRules(MatchFormat.Create(), MatchTime),
                TeamIds = Teams.Select(x => x.Id).ToList(),
                Date = Date.GetValueOrDefault().ToUtcDateTime(Time),
                IsPostponed = IsPostponed,
                PostponedDate = ShowPostponedDate ? PostponedDate?.Date.ToUtcDateTime(PostponedTime ?? Parent?.GetDefaultDateTime().TimeOfDay ?? DateTime.Now.TimeOfDay) : null,
            };

        protected override void RefreshFrom(IRound item)
        {
            if (Parent is not null && item is Knockout knockout)
            {
                TeamSelectionViewModel.Reset();
                TeamSelectionViewModel.UpdateSource(Parent.GetAvailableTeams());

                Name = knockout.Name;
                ShortName = knockout.ShortName;
                Date = knockout.OriginDate.Date;
                Teams.Set(Parent.GetAvailableTeams().Where(x => item.Teams.Select(y => y.Id).Contains(x.Id)).ToList());
                Time = knockout.OriginDate.ToLocalTime().TimeOfDay;
                IsPostponed = knockout.IsPostponed;
                PostponedDate = knockout.OriginDate == knockout.Date ? null : knockout.Date.Date;
                PostponedTime = knockout.OriginDate == knockout.Date ? Parent.GetDefaultDateTime().TimeOfDay : knockout.Date.ToLocalTime().TimeOfDay;
                ShowPostponedDate = PostponedDate.HasValue;
                MatchTime = knockout.Rules.MatchTime;
                MatchFormat.Load(knockout.Rules.MatchFormat);
                ShowRemovingTeamWarning = item.GetAllMatches().Any();
            }
        }

        private void ValidateAndAddTeam()
        {
            AddSelectedTeam();
            TeamSelectionViewModel.SelectedItem = null;
            TeamSelectionViewModel.TextSearch = null;
        }

        private void AddSelectedTeam()
        {
            if (Parent is not null && TeamSelectionViewModel.SelectedItem?.Id is not null)
            {
                var team = Parent.GetAvailableTeams().GetById(TeamSelectionViewModel.SelectedItem.Id.Value);
                if (!Teams.Contains(team))
                    Teams.Add(team);
            }
        }
    }
}
