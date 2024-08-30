// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Utilities;
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
            StartDate = schedulingParameters.StartDate;
            (IntervalValue, IntervalUnit) = schedulingParameters.Interval.Simplify();
            DateRules.Load(schedulingParameters.DateRules);
            TimeRules.Load(schedulingParameters.TimeRules);
        }

        public BuildDatesParametersDto ProvideBuildDatesParameters(int countMatchdays, int countMatchesByMatchday, TimeOnly defaultTime) => new BuildAutomaticDatesParametersDto
        {
            StartDate = StartDate,
            IntervalValue = IntervalValue.GetValueOrDefault(),
            IntervalUnit = IntervalUnit,
            DefaultTime = defaultTime,
            DateRules = DateRules.Rules.Select(x => x.ProvideRule()).ToList(),
            TimeRules = TimeRules.Rules.Select(x => x.ProvideRule()).ToList()
        };
    }
}
