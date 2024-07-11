// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using MyClub.CrossCutting.Localization;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.UI.Resources;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal partial class MatchdaysEditionAutomaticViewModel : EditableObject
    {
        public MatchdaysEditionAutomaticViewModel()
        {
            ValidationRules.Add<MatchdaysEditionAutomaticViewModel, DateTime?>(x => x.EndDate, MessageResources.FieldXIsRequiredError.FormatWith(MyClubResources.EndDate), x => !UseEndDate || x.HasValue);
            ValidationRules.Add<MatchdaysEditionAutomaticViewModel, DateTime?>(x => x.EndDate, MessageResources.FieldEndDateMustBeUpperOrEqualsThanStartDateError, x => !UseEndDate || !x.HasValue || x.Value > StartDate);
            ValidationRules.Add<MatchdaysEditionAutomaticViewModel, int?>(x => x.CountMatchdays, MessageResources.FieldXIsRequiredError, x => UseEndDate || x.HasValue);
        }

        public EditableDateRulesViewModel DateRules { get; } = new();

        public EditableTimeRulesViewModel TimeRules { get; } = new();

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public DateTime StartDisplayDate { get; private set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public DateTime EndDisplayDate { get; private set; }

        [IsRequired]
        [Display(Name = nameof(StartDate), ResourceType = typeof(MyClubResources))]
        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public int? CountMatchdays { get; set; }

        public bool UseEndDate { get; set; }

        public void Reset(Period allowPeriod, int defaultCountMatchdays)
        {
            UseEndDate = false;
            StartDate = DateTime.Today;
            EndDate = allowPeriod.End;
            CountMatchdays = defaultCountMatchdays;
            StartDisplayDate = allowPeriod.Start;
            EndDisplayDate = allowPeriod.End;
            DateRules.Rules.Clear();
            TimeRules.Rules.Clear();
        }

        public IEnumerable<(DateTime, TimeSpan?)> ProvideDates()
        {
            var result = new List<(DateTime, TimeSpan?)>();
            var date = StartDate ?? DateTime.Today;
            DateTime? previousDate = null;
            while (!isEnd())
            {
                if (DateRules.Rules.All(x => x.Match(date, previousDate)))
                {
                    result.Add((date, ProvideTime(date)));
                    previousDate = date;
                }
                date = date.Date.AddDays(1);
            }

            return result;

            bool isEnd() => UseEndDate && date > EndDate || !UseEndDate && result.Count >= CountMatchdays;
        }

        private TimeSpan? ProvideTime(DateTime date)
        {
            foreach (var rule in TimeRules.Rules)
            {
                var time = rule.ProvideTime(date, 0);

                if (time is not null)
                    return time;
            }

            return null;
        }
    }
}
