// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Utilities;
using MyNet.Utilities.Units;

namespace MyClub.Scorer.Wpf.ViewModels.BuildAssistant
{
    internal class BuildAutomaticViewModel : EditableObject, IBuildMethodViewModel
    {
        public EditableDateRulesViewModel DateRules { get; } = new();

        public EditableTimeRulesViewModel TimeRules { get; } = new();

        [IsRequired]
        [Display(Name = nameof(StartDate), ResourceType = typeof(MyClubResources))]
        public DateTime? StartDate { get; set; }

        [IsRequired]
        [Display(Name = "Interval", ResourceType = typeof(MyClubResources))]
        public int? IntervalValue { get; set; }

        [IsRequired]
        [Display(Name = "Interval", ResourceType = typeof(MyClubResources))]
        public TimeUnit IntervalUnit { get; set; }

        public void Reset(DateTime startDate)
        {
            Refresh(startDate);
            IntervalValue = 1;
            IntervalUnit = TimeUnit.Day;
            DateRules.Rules.Clear();
            TimeRules.Rules.Clear();
        }

        public void Refresh(DateTime startDate) => StartDate = startDate.Date;

        public BuildDatesParametersDto ProvideBuildDatesParameters(int countMatchdays, int countMatchesByMatchday, TimeSpan defaultTime) => new BuildAutomaticParametersDto
        {
            Dates = ProvideDates(countMatchdays, countMatchesByMatchday, defaultTime)
        };

        private List<(DateTime, IEnumerable<DateTime>)> ProvideDates(int countMatchdays, int countMatchesByMatchday, TimeSpan defaultTime)
        {
            var result = new List<(DateTime, IEnumerable<DateTime>)>();
            var date = StartDate ?? DateTime.Today;
            DateTime? previousDate = null;
            while (result.Count < countMatchdays)
            {
                if (DateRules.Rules.All(x => x.Match(date, previousDate)))
                {
                    var times = ProvideTimes(date, countMatchesByMatchday, defaultTime);
                    result.Add((date, times));
                    previousDate = date;
                }
                date = date.Date.Add(IntervalValue.GetValueOrDefault(), IntervalUnit);
            }

            return result;
        }

        private List<DateTime> ProvideTimes(DateTime date, int countMatchesByMatchday, TimeSpan defaultTime)
        {
            var result = new List<DateTime>();

            for (var matchIndex = 0; matchIndex < countMatchesByMatchday; matchIndex++)
            {
                var time = TimeRules.Rules.Select(x => x.ProvideTime(date, matchIndex)).FirstOrDefault(x => x is not null) ?? defaultTime;
                result.Add(date.ToLocalDateTime(time));
            }

            return result;
        }
    }
}
