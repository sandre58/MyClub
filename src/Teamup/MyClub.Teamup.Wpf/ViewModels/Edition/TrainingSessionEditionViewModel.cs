// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using MyNet.Utilities;
using MyNet.DynamicData.Extensions;
using MyNet.Observable.Attributes;
using MyNet.UI.Resources;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Domain.TrainingAggregate;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class TrainingSessionEditionViewModel : EntityEditionViewModel<TrainingSession, TrainingSessionDto, TrainingSessionService>
    {
        [Display(Name = nameof(Theme), ResourceType = typeof(MyClubResources))]
        public string? Theme { get; set; }

        [Display(Name = nameof(Place), ResourceType = typeof(MyClubResources))]
        public string? Place { get; set; }

        [IsRequired]
        [Display(Name = nameof(Date), ResourceType = typeof(MyClubResources))]
        public DateTime? Date { get; set; }

        [IsRequired]
        [ValidateProperty(nameof(EndTime))]
        [Display(Name = nameof(StartTime), ResourceType = typeof(MyClubResources))]
        public TimeSpan StartTime { get; set; }

        [IsRequired]
        [ValidateProperty(nameof(StartTime))]
        [Display(Name = nameof(EndTime), ResourceType = typeof(MyClubResources))]
        public TimeSpan EndTime { get; set; }

        [Display(Name = nameof(IsCancelled), ResourceType = typeof(MyClubResources))]
        public bool IsCancelled { get; set; }

        public IEnumerable? SelectedTeamIds { get; set; }

        public ObservableCollection<string> Stages { get; } = [];

        public ObservableCollection<string> TechnicalGoals { get; } = [];

        public ObservableCollection<string> TacticalGoals { get; } = [];

        public ObservableCollection<string> PhysicalGoals { get; } = [];

        public ObservableCollection<string> MentalGoals { get; } = [];

        public TrainingSessionEditionViewModel(
            TrainingSessionService trainingSessionService)
            : base(trainingSessionService)
        {
            ValidationRules.AddNotNull<TrainingSessionEditionViewModel, IEnumerable?>(x => x.SelectedTeamIds, MyClubResources.AnySelectedSquadsError, x => x is not null && x.OfType<Guid>().Any());
            ValidationRules.AddNotNull<TrainingSessionEditionViewModel, TimeSpan>(x => x.StartTime, MessageResources.FieldStartDateMustBeLowerOrEqualsThanEndDateError, x => x <= EndTime);
            ValidationRules.Add<TrainingSessionEditionViewModel, TimeSpan>(x => x.EndTime, MessageResources.FieldEndDateMustBeUpperOrEqualsThanStartDateError, x => StartTime <= x);
        }

        protected override TrainingSessionDto ToDto()
            => new()
            {
                Id = ItemId,
                Place = Place,
                Theme = Theme,
                IsCancelled = IsCancelled,
                StartDate = (Date ?? DateTime.Today).ToUtcDateTime(StartTime),
                EndDate = (Date ?? DateTime.Today).ToUtcDateTime(EndTime),
                TeamIds = SelectedTeamIds?.OfType<Guid>().ToList(),
                Stages = [.. Stages],
                MentalGoals = [.. MentalGoals],
                PhysicalGoals = [.. PhysicalGoals],
                TacticalGoals = [.. TacticalGoals],
                TechnicalGoals = [.. TechnicalGoals]
            };

        protected override void RefreshFrom(TrainingSession item)
        {
            Place = item.Place;
            Theme = item.Theme;
            IsCancelled = item.IsCancelled;
            Date = item.Start.Date.ToLocalTime();
            StartTime = item.Start.ToLocalTime().TimeOfDay;
            EndTime = item.End.ToLocalTime().TimeOfDay;
            SelectedTeamIds = item.TeamIds.ToList();
            Stages.Set([.. item.Stages]);
            MentalGoals.Set([.. item.MentalGoals]);
            PhysicalGoals.Set([.. item.PhysicalGoals]);
            TacticalGoals.Set([.. item.TacticalGoals]);
            TechnicalGoals.Set([.. item.TechnicalGoals]);
        }

        protected override void ResetItem()
        {
            var defaultValues = CrudService.NewTrainingSession(DateTime.Today);

            Place = defaultValues.Place;
            Theme = defaultValues.Theme;
            IsCancelled = defaultValues.IsCancelled;
            Date = defaultValues.StartDate.Date.ToLocalTime();
            StartTime = defaultValues.StartDate.ToLocalTime().TimeOfDay;
            EndTime = defaultValues.EndDate.ToLocalTime().TimeOfDay;
            SelectedTeamIds = defaultValues.TeamIds?.ToList();
            Stages.Clear();
            MentalGoals.Clear();
            PhysicalGoals.Clear();
            TacticalGoals.Clear();
            TechnicalGoals.Clear();
        }
    }
}
