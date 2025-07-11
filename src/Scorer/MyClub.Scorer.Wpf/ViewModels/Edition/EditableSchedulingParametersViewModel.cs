// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.Scheduling;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Resources;
using MyNet.UI.ViewModels.Workspace;
using MyNet.Utilities;
using MyNet.Utilities.Localization;
using MyNet.Utilities.Units;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditableSchedulingParametersViewModel : NavigableWorkspaceViewModel
    {
        public EditableSchedulingParametersViewModel(ISourceProvider<IEditableStadiumViewModel> stadiums)
        {
            VenueRules = new(stadiums.Source);
            ValidationRules.Add<EditableSchedulingParametersViewModel, DateOnly?>(x => x.EndDate, MessageResources.FieldEndDateMustBeUpperOrEqualsThanStartDateError, x => !x.HasValue || !StartDate.HasValue || x.Value > StartDate);

            Reset();
        }

        public SchedulingParameters Create()
            => new(StartDate.GetValueOrDefault().ToDateTime(StartTime.GetValueOrDefault()).ToUniversalTime().ToDate(),
                   EndDate.GetValueOrDefault().ToDateTime(StartTime.GetValueOrDefault()).ToUniversalTime().ToDate(),
                   StartDate.GetValueOrDefault().ToDateTime(StartTime.GetValueOrDefault()).ToUniversalTime().ToTime(),
                   RotationTimeValue.GetValueOrDefault().ToTimeSpan(RotationTimeUnit),
                   RestTimeValue.GetValueOrDefault().ToTimeSpan(RestTimeUnit),
                   UseHomeVenue,
                   AsSoonAsPossible,
                   IntervalValue.GetValueOrDefault().ToTimeSpan(IntervalUnit),
                   ScheduleByStage,
                   AsSoonAsPossible ? AsSoonAsPossibleRules.Rules.Select(x => x.ProvideRule()).SelectMany(x => x.ConvertToTimeZone(GlobalizationService.Current.TimeZone, TimeZoneInfo.Utc)).ToList() : [],
                   AsSoonAsPossible ? [] : DateRules.Rules.Select(x => x.ProvideRule()).SelectMany(x => x.ConvertToTimeZone(GlobalizationService.Current.TimeZone, TimeZoneInfo.Utc)).ToList(),
                   AsSoonAsPossible ? [] : TimeRules.Rules.Select(x => x.ProvideRule()).SelectMany(x => x.ConvertToTimeZone(GlobalizationService.Current.TimeZone, TimeZoneInfo.Utc)).ToList(),
                   !UseHomeVenue ? VenueRules.Rules.Select(x => x.ProvideRule()).ToList() : []);

        [IsRequired]
        [ValidateProperty(nameof(EndDate))]
        [Display(Name = nameof(StartDate), ResourceType = typeof(MyClubResources))]
        public DateOnly? StartDate { get; set; }

        [IsRequired]
        [Display(Name = nameof(EndDate), ResourceType = typeof(MyClubResources))]
        public DateOnly? EndDate { get; set; }

        [IsRequired]
        [Display(Name = nameof(StartTime), ResourceType = typeof(MyClubResources))]
        public TimeOnly? StartTime { get; set; }

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
        [Display(Name = nameof(ScheduleByStage), ResourceType = typeof(MyClubResources))]
        public bool ScheduleByStage { get; set; }

        [CanBeValidated(false)]
        public EditableAsSoonAsPossibleDateSchedulingRulesViewModel AsSoonAsPossibleRules { get; } = new();

        [CanBeValidated(false)]
        public EditableAutomaticDateSchedulingRulesViewModel DateRules { get; } = new();

        [CanBeValidated(false)]
        public EditableAutomaticTimeSchedulingRulesViewModel TimeRules { get; } = EditableAutomaticTimeSchedulingRulesViewModel.Default;

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

        protected override void ResetCore() => Load(SchedulingParameters.Default);

        public void Load(SchedulingParameters schedulingParameters)
        {
            StartDate = schedulingParameters.GetCurrentStartDate();
            EndDate = schedulingParameters.GetCurrentEndDate();
            StartTime = schedulingParameters.GetCurrentStartTime();
            (RotationTimeValue, RotationTimeUnit) = schedulingParameters.RotationTime.Simplify();
            (RestTimeValue, RestTimeUnit) = schedulingParameters.RestTime.Simplify();
            UseHomeVenue = schedulingParameters.UseHomeVenue;
            AsSoonAsPossible = schedulingParameters.AsSoonAsPossible;
            (IntervalValue, IntervalUnit) = schedulingParameters.Interval.Simplify();
            ScheduleByStage = schedulingParameters.ScheduleByStage;

            if (AsSoonAsPossible)
            {
                AsSoonAsPossibleRules.Load(schedulingParameters.AsSoonAsPossibleRules.SelectMany(x => x.ConvertToTimeZone(TimeZoneInfo.Utc, GlobalizationService.Current.TimeZone)));
                DateRules.Rules.Clear();
                TimeRules.Rules.Clear();
            }
            else
            {
                AsSoonAsPossibleRules.Rules.Clear();
                DateRules.Load(schedulingParameters.DateRules.SelectMany(x => x.ConvertToTimeZone(TimeZoneInfo.Utc, GlobalizationService.Current.TimeZone)));
                TimeRules.Load(schedulingParameters.TimeRules.SelectMany(x => x.ConvertToTimeZone(TimeZoneInfo.Utc, GlobalizationService.Current.TimeZone)));
            }

            if (!UseHomeVenue)
                VenueRules.Load(schedulingParameters.VenueRules);
            else
                VenueRules.Rules.Clear();
        }

        public bool CanScheduleAutomatic() => AsSoonAsPossible || DateRules.Rules.Count > 0;

        public bool CanScheduleVenuesAutomatic() => UseHomeVenue || VenueRules.Rules.Count > 0;
    }
}
