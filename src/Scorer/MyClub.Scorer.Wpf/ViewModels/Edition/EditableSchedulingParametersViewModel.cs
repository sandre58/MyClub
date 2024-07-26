// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.UI.Resources;
using MyNet.Utilities;
using MyNet.Utilities.Units;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditableSchedulingParametersViewModel : EditableObject
    {
        public EditableSchedulingParametersViewModel(ReadOnlyObservableCollection<StadiumViewModel> stadiums)
        {
            VenueRules = new(stadiums);
            ValidationRules.Add<EditableSchedulingParametersViewModel, DateTime?>(x => x.EndDate, MessageResources.FieldEndDateMustBeUpperOrEqualsThanStartDateError, x => !x.HasValue || !StartDate.HasValue || x.Value > StartDate);
        }

        public SchedulingParameters Create()
            => new(StartDate.GetValueOrDefault().ToUniversalTime(),
                   EndDate.GetValueOrDefault().ToUniversalTime(),
                   StartTime.GetValueOrDefault(),
                   RotationTimeValue.GetValueOrDefault().ToTimeSpan(RotationTimeUnit),
                   RestTimeValue.GetValueOrDefault().ToTimeSpan(RestTimeUnit),
                   UseHomeVenue,
                   AsSoonAsPossible,
                   IntervalValue.GetValueOrDefault().ToTimeSpan(IntervalUnit),
                   ScheduleByParent,
                   AsSoonAsPossible ? AsSoonAsPossibleRules.Rules.Select(x => x.ProvideRule()).ToList() : [],
                   AsSoonAsPossible ? [] : DateRules.Rules.Select(x => x.ProvideRule()).ToList(),
                   AsSoonAsPossible ? [] : TimeRules.Rules.Select(x => x.ProvideRule()).ToList(),
                   !UseHomeVenue ? VenueRules.Rules.Select(x => x.ProvideRule()).ToList() : []);

        [IsRequired]
        [ValidateProperty(nameof(EndDate))]
        [Display(Name = nameof(StartDate), ResourceType = typeof(MyClubResources))]
        public DateTime? StartDate { get; set; }

        [IsRequired]
        [Display(Name = nameof(EndDate), ResourceType = typeof(MyClubResources))]
        public DateTime? EndDate { get; set; }

        [IsRequired]
        [Display(Name = nameof(StartTime), ResourceType = typeof(MyClubResources))]
        public TimeSpan? StartTime { get; set; }

        [IsRequired]
        [Display(Name = "RotationTime", ResourceType = typeof(MyClubResources))]
        public int? RotationTimeValue { get; set; }

        [IsRequired]
        [Display(Name = "RotationTime", ResourceType = typeof(MyClubResources))]
        public TimeUnit RotationTimeUnit { get; set; }

        [IsRequired]
        [Display(Name = "RestTime", ResourceType = typeof(MyClubResources))]
        public int? RestTimeValue { get; set; }

        [IsRequired]
        [Display(Name = "RestTime", ResourceType = typeof(MyClubResources))]
        public TimeUnit RestTimeUnit { get; set; }

        [IsRequired]
        [Display(Name = "UseHomeVenue", ResourceType = typeof(MyClubResources))]
        public bool UseHomeVenue { get; set; }

        [IsRequired]
        [Display(Name = "AsSoonAsPossible", ResourceType = typeof(MyClubResources))]
        public bool AsSoonAsPossible { get; set; }

        [IsRequired]
        [Display(Name = "Interval", ResourceType = typeof(MyClubResources))]
        public int? IntervalValue { get; set; }

        [IsRequired]
        [Display(Name = "Interval", ResourceType = typeof(MyClubResources))]
        public TimeUnit IntervalUnit { get; set; }

        [IsRequired]
        [Display(Name = nameof(ScheduleByParent), ResourceType = typeof(MyClubResources))]
        public bool ScheduleByParent { get; set; }

        [CanBeValidated(false)]
        public EditableAsSoonAsPossibleDateSchedulingRulesViewModel AsSoonAsPossibleRules { get; } = new();

        [CanBeValidated(false)]
        public EditableAutomaticDateSchedulingRulesViewModel DateRules { get; } = new();

        [CanBeValidated(false)]
        public EditableAutomaticTimeSchedulingRulesViewModel TimeRules { get; } = new();

        [CanBeValidated(false)]
        public EditableVenueSchedulingRulesViewModel VenueRules { get; }

        public override bool ValidateProperties() => base.ValidateProperties() && (AsSoonAsPossible && AsSoonAsPossibleRules.ValidateProperties() || !AsSoonAsPossible && DateRules.ValidateProperties() && TimeRules.ValidateProperties()) && (UseHomeVenue || VenueRules.ValidateProperties());

        public override IEnumerable<string> GetErrors()
        {
            var result = base.GetErrors().ToList();

            if (AsSoonAsPossible)
                result.AddRange(AsSoonAsPossibleRules.GetErrors());
            else
            {
                result.AddRange(DateRules.GetErrors());
                result.AddRange(TimeRules.GetErrors());
            }

            if (!UseHomeVenue)
                result.AddRange(VenueRules.GetErrors());

            return result;
        }
        public void Load(SchedulingParameters schedulingParameters)
        {
            StartDate = schedulingParameters.StartDate.ToLocalTime();
            EndDate = schedulingParameters.EndDate.ToLocalTime();
            StartTime = schedulingParameters.StartTime;
            (RotationTimeValue, RotationTimeUnit) = schedulingParameters.RotationTime.Simplify();
            (RestTimeValue, RestTimeUnit) = schedulingParameters.RestTime.Simplify();
            UseHomeVenue = schedulingParameters.UseHomeVenue;
            AsSoonAsPossible = schedulingParameters.AsSoonAsPossible;
            (IntervalValue, IntervalUnit) = schedulingParameters.Interval.Simplify();
            ScheduleByParent = schedulingParameters.ScheduleByParent;

            if (AsSoonAsPossible)
            {
                AsSoonAsPossibleRules.Load(schedulingParameters.AsSoonAsPossibleRules);
                DateRules.Rules.Clear();
                TimeRules.Rules.Clear();
            }
            else
            {
                AsSoonAsPossibleRules.Rules.Clear();
                DateRules.Load(schedulingParameters.DateRules);
                TimeRules.Load(schedulingParameters.TimeRules);
            }

            if (!UseHomeVenue)
                VenueRules.Load(schedulingParameters.VenueRules);
            else
                VenueRules.Rules.Clear();
        }
    }
}
