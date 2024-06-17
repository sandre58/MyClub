// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using MyClub.CrossCutting.Localization;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections;
using MyNet.UI.Collections;
using MyNet.UI.Commands;
using MyNet.UI.Resources;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal partial class MatchdaysEditionAutomaticViewModel : EditableObject
    {
        public MatchdaysEditionAutomaticViewModel()
        {
            AddRuleCommand = CommandsManager.CreateNotNull<AvailableRuleViewModel>(x => AddDate(x.Item), x => x.CanAdd());
            RemoveRuleCommand = CommandsManager.CreateNotNull<IDateRuleViewModel>(RemoveDate);
            AddTimeRuleCommand = CommandsManager.CreateNotNull<AvailableRuleViewModel>(x => AddTimeRule(x.Item), x => x.CanAdd());
            RemoveTimeRuleCommand = CommandsManager.CreateNotNull<ITimeRuleViewModel>(RemoveTimeRule);
            MoveUpTimeRuleCommand = CommandsManager.CreateNotNull<ITimeRuleViewModel>(MoveUpTimeRule, CanMoveUpTimeRule);
            MoveDownTimeRuleCommand = CommandsManager.CreateNotNull<ITimeRuleViewModel>(MoveDownTimeRule, CanMoveDownTimeRule);

            AvailableRules = new Collection<AvailableRuleViewModel>()
            {
                new(typeof(DayOfWeeksRuleViewModel), MyClubResources.AllowDaysOfWeekDateRule, () => !Rules.OfType<DayOfWeeksRuleViewModel>().Any()),
                new(typeof(ExcludeDateRuleViewModel), MyClubResources.ExcludeDateDateRule, () => true),
                new(typeof(ExcludeDatesRangeRuleViewModel), MyClubResources.ExcludeDatesRangeDateRule, () => true),
                new(typeof(DateIntervalRuleViewModel), MyClubResources.IntervalDateRule, () =>  !Rules.OfType<DateIntervalRuleViewModel>().Any()),
            };
            AvailableTimeRules = new Collection<AvailableRuleViewModel>()
            {
                new(typeof(TimeOfDayRuleViewModel), MyClubResources.TimeOfDayTimeRule, () => true),
                new(typeof(TimeOfDateRuleViewModel), MyClubResources.TimeOfDateTimeRule, () => true),
                new(typeof(TimeOfDateRangeRuleViewModel), MyClubResources.TimeOfDateRangeTimeRule, () => true),
            };

            ValidationRules.Add<MatchdaysEditionAutomaticViewModel, DateTime?>(x => x.EndDate, MessageResources.FieldXIsRequiredError.FormatWith(MyClubResources.EndDate), x => !UseEndDate || x.HasValue);
            ValidationRules.Add<MatchdaysEditionAutomaticViewModel, DateTime?>(x => x.EndDate, MessageResources.FieldEndDateMustBeUpperOrEqualsThanStartDateError, x => !UseEndDate || !x.HasValue || x.Value > StartDate);
            ValidationRules.Add<MatchdaysEditionAutomaticViewModel, int?>(x => x.CountMatchdays, MessageResources.FieldXIsRequiredError, x => UseEndDate || x.HasValue);
        }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public IReadOnlyCollection<AvailableRuleViewModel> AvailableRules { get; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public IReadOnlyCollection<AvailableRuleViewModel> AvailableTimeRules { get; }

        [HasAnyItems]
        [Display(Name = nameof(Rules), ResourceType = typeof(MyClubResources))]
        public UiObservableCollection<IDateRuleViewModel> Rules { get; } = [];

        public UiObservableCollection<ITimeRuleViewModel> TimeRules { get; } = [];

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

        public ICommand AddRuleCommand { get; }

        public ICommand RemoveRuleCommand { get; }

        public ICommand AddTimeRuleCommand { get; }

        public ICommand RemoveTimeRuleCommand { get; }

        public ICommand MoveDownTimeRuleCommand { get; }

        public ICommand MoveUpTimeRuleCommand { get; }

        private void AddDate(Type type)
        {
            var rule = (IDateRuleViewModel?)Activator.CreateInstance(type);

            if (rule is not null)
                Rules.Add(rule);
        }

        private void RemoveDate(IDateRuleViewModel rule) => Rules.Remove(rule);

        private void AddTimeRule(Type type)
        {
            var rule = (ITimeRuleViewModel?)Activator.CreateInstance(type);

            if (rule is not null)
                TimeRules.Add(rule);
        }

        private void RemoveTimeRule(ITimeRuleViewModel rule) => TimeRules.Remove(rule);

        public virtual void MoveUpTimeRule(ITimeRuleViewModel filter)
        {
            var index = TimeRules.IndexOf(filter);

            if (index > 0)
                TimeRules.Swap(index, index - 1);
        }

        protected virtual bool CanMoveUpTimeRule(ITimeRuleViewModel filter) => TimeRules.IndexOf(filter) > 0;

        public virtual void MoveDownTimeRule(ITimeRuleViewModel filter)
        {
            var index = TimeRules.IndexOf(filter);

            if (index > -1 && index < TimeRules.Count - 1)
                TimeRules.Swap(index, index + 1);
        }

        protected virtual bool CanMoveDownTimeRule(ITimeRuleViewModel filter) => TimeRules.IndexOf(filter) < TimeRules.Count - 1;

        public void Reset(Period allowPeriod, int defaultCountMatchdays)
        {
            UseEndDate = false;
            StartDate = DateTime.Today;
            EndDate = allowPeriod.End;
            CountMatchdays = defaultCountMatchdays;
            StartDisplayDate = allowPeriod.Start;
            EndDisplayDate = allowPeriod.End;
            Rules.Clear();
            TimeRules.Clear();
        }

        public IEnumerable<(DateTime, TimeSpan?)> ProvideDates()
        {
            var result = new List<(DateTime, TimeSpan?)>();
            var date = StartDate ?? DateTime.Today;
            DateTime? previousDate = null;
            while (!isEnd())
            {
                if (Rules.All(x => x.Match(date, previousDate)))
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
            foreach (var rule in TimeRules)
            {
                var time = rule.ProvideTime(date);

                if (time is not null)
                    return time;
            }

            return null;
        }
    }
}
