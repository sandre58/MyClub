// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Utilities;
using MyNet.Utilities.Localization;
using MyNet.Utilities.Units;

namespace MyClub.Scorer.Wpf.ViewModels.BuildAssistant
{
    internal class DatesSchedulingAutomaticViewModel : EditableObject, IDatesSchedulingMethodViewModel
    {
        public EditableAutomaticDateSchedulingRulesViewModel DateRules { get; } = new();

        public EditableAutomaticTimeSchedulingRulesViewModel TimeRules { get; } = EditableAutomaticTimeSchedulingRulesViewModel.Default;

        [IsRequired]
        [Display(Name = nameof(StartDate), ResourceType = typeof(MyClubResources))]
        public DateOnly? StartDate { get; set; }

        [IsRequired]
        [Display(Name = "Interval", ResourceType = typeof(MyClubResources))]
        public int? IntervalValue { get; set; }

        [IsRequired]
        [Display(Name = "Interval", ResourceType = typeof(MyClubResources))]
        public TimeUnit IntervalUnit { get; set; }

        public void Reset(DateTime startDate)
        {
            StartDate = startDate.ToDate();
            IntervalValue = 1;
            IntervalUnit = TimeUnit.Day;
            DateRules.Rules.Clear();
            TimeRules.Rules.Clear();
        }

        public void Load(SchedulingParameters schedulingParameters)
        {
            StartDate = schedulingParameters.GetCurrentStartDate();
            (IntervalValue, IntervalUnit) = schedulingParameters.Interval.Simplify();
            DateRules.Load(schedulingParameters.DateRules.SelectMany(x => x.ConvertToTimeZone(TimeZoneInfo.Utc, GlobalizationService.Current.TimeZone)));
            TimeRules.Load(schedulingParameters.TimeRules.SelectMany(x => x.ConvertToTimeZone(TimeZoneInfo.Utc, GlobalizationService.Current.TimeZone)));
        }

        public BuildDatesParametersDto ProvideBuildDatesParameters(int countMatchdays, int countMatchesByMatchday, TimeOnly defaultTime) => new BuildAutomaticDatesParametersDto
        {
            StartDate = StartDate.GetValueOrDefault().At(defaultTime).ToUniversalTime().ToDate(),
            IntervalValue = IntervalValue.GetValueOrDefault(),
            IntervalUnit = IntervalUnit,
            DefaultTime = StartDate.GetValueOrDefault().At(defaultTime).ToUniversalTime().ToTime(),
            DateRules = DateRules.Rules.Select(x => x.ProvideRule()).SelectMany(x => x.ConvertToTimeZone(GlobalizationService.Current.TimeZone, TimeZoneInfo.Utc)).ToList(),
            TimeRules = TimeRules.Rules.Select(x => x.ProvideRule()).SelectMany(x => x.ConvertToTimeZone(GlobalizationService.Current.TimeZone, TimeZoneInfo.Utc)).ToList()
        };
    }
}
