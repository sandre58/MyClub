// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;
using MyNet.Utilities;
using MyNet.Observable.Attributes;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Wpf.Extensions;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class MatchdayEditionViewModel(MatchdayService matchdayService) : EntityEditionViewModel<Matchday, MatchdayDto, MatchdayService>(matchdayService)
    {
        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public string? SubTitle => Equals(Parent, Competition) ? null : Parent?.Name;

        public IMatchdayParent? Parent { get; private set; }

        public CompetitionViewModel? Competition { get; private set; }

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

        public void New(IMatchdayParent parent, Action? initialize = null)
        {
            Parent = parent;
            Competition = parent.GetCompetition();
            New(initialize);
            UpdateTitle();
        }

        public void Load(IMatchdayParent parent, Guid matchdayId)
        {
            Load(matchdayId);
            Parent = parent;
            Competition = parent.GetCompetition();
            UpdateTitle();
        }

        protected override string CreateTitle()
        {
            RaisePropertyChanged(nameof(SubTitle));
            return Competition?.Name ?? string.Empty;
        }

        protected override void ResetItem()
        {
            if (Parent is not null)
            {
                var defaultValues = CrudService.NewMatchday(Parent.Id);
                Date = defaultValues.Date.Date;
                Time = defaultValues.Date.ToLocalTime().TimeOfDay;
                ShortName = defaultValues.ShortName.OrEmpty();
                Name = defaultValues.Name.OrEmpty();
            }
        }
        protected override MatchdayDto ToDto()
            => new()
            {
                Id = ItemId,
                ParentId = Parent?.Id,
                Name = Name,
                ShortName = ShortName,
                Date = Date.GetValueOrDefault().ToUtc(Time),
                IsPostponed = IsPostponed,
                PostponedDate = ShowPostponedDate ? PostponedDate?.Date.ToUtc(PostponedTime ?? Parent?.GetDefaultDateTime().TimeOfDay ?? DateTime.Now.TimeOfDay) : null,
            };

        protected override void RefreshFrom(Matchday item)
        {
            if (Parent is not null)
            {
                Name = item.Name;
                ShortName = item.ShortName;
                Date = item.OriginDate.Date;
                Time = item.OriginDate.ToLocalTime().TimeOfDay;
                IsPostponed = item.IsPostponed;
                PostponedDate = item.OriginDate == item.Date ? null : item.Date.Date;
                PostponedTime = item.OriginDate == item.Date ? Parent.GetDefaultDateTime().TimeOfDay : item.Date.ToLocalTime().TimeOfDay;
                ShowPostponedDate = PostponedDate.HasValue;
            }
        }
    }
}
