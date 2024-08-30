// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using DynamicData;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Domain.Scheduling;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.UI.Commands;
using MyNet.UI.Resources;
using MyNet.UI.ViewModels.Rules;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using MyNet.Utilities.Units;
using PropertyChanged;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal interface IEditableAsSoonAsPossibleDateSchedulingRuleViewModel : IEditableRule
    {
        IAvailableDateSchedulingRule ProvideRule();
    }

    internal interface IEditableAutomaticDateSchedulingRuleViewModel : IEditableRule
    {
        IDateSchedulingRule ProvideRule();
    }

    internal class EditableAsSoonAsPossibleDateSchedulingRulesViewModel : EditableRulesViewModel<IEditableAsSoonAsPossibleDateSchedulingRuleViewModel>
    {
        public EditableAsSoonAsPossibleDateSchedulingRulesViewModel()
        => AvailableRules.AddRange([
                new AvailableRule<IEditableAsSoonAsPossibleDateSchedulingRuleViewModel>(MyClubResources.AllowTimePeriodsDateRule, () => new EditableIncludeTimePeriodsRuleViewModel(), () => !Rules.OfType<EditableIncludeTimePeriodsRuleViewModel>().Any()),
                new AvailableRule<IEditableAsSoonAsPossibleDateSchedulingRuleViewModel>(MyClubResources.AllowDaysOfWeekDateRule, () => new EditableIncludeDaysOfWeekRuleViewModel(), () => !Rules.OfType<EditableIncludeDaysOfWeekRuleViewModel>().Any()),
                new AvailableRule<IEditableAsSoonAsPossibleDateSchedulingRuleViewModel>(MyClubResources.ExcludeDatesRangeDateRule, () => new EditableExcludeDatesRangeRuleViewModel()),
           ]);

        internal void Load(IEnumerable<IAvailableDateSchedulingRule> rules)
            => Rules.Set(rules.Select(x =>
                {
                    switch (x)
                    {
                        case IncludeDaysOfWeekRule includeDaysOfWeekRule:
                            return (IEditableAsSoonAsPossibleDateSchedulingRuleViewModel)new EditableIncludeDaysOfWeekRuleViewModel()
                            {
                                SelectedDays = includeDaysOfWeekRule.DaysOfWeek,
                            };

                        case ExcludeDatesRangeRule excludePeriodRule:
                            return new EditableExcludeDatesRangeRuleViewModel()
                            {
                                StartDate = excludePeriodRule.StartDate,
                                EndDate = excludePeriodRule.EndDate
                            };

                        case IncludeTimePeriodsRule includeTimePeriodsRule:
                            var rule = new EditableIncludeTimePeriodsRuleViewModel();
                            rule.TimePeriods.Set(includeTimePeriodsRule.Periods.Select(x => new EditableTimePeriodViewModel()
                            {
                                StartTime = x.Start,
                                EndTime = x.End
                            }));
                            return rule;

                        default:
                            throw new NotImplementedException();
                    }
                }));
    }

    internal class EditableAutomaticDateSchedulingRulesViewModel : EditableRulesViewModel<IEditableAutomaticDateSchedulingRuleViewModel>
    {
        public EditableAutomaticDateSchedulingRulesViewModel()
        => AvailableRules.AddRange([
                new AvailableRule<IEditableAutomaticDateSchedulingRuleViewModel>(MyClubResources.AllowDaysOfWeekDateRule, () => new EditableIncludeDaysOfWeekRuleViewModel(), () => !Rules.OfType<EditableIncludeDaysOfWeekRuleViewModel>().Any()),
                new AvailableRule<IEditableAutomaticDateSchedulingRuleViewModel>(MyClubResources.ExcludeDateDateRule, () => new EditableExcludeDateRuleViewModel()),
                new AvailableRule<IEditableAutomaticDateSchedulingRuleViewModel>(MyClubResources.ExcludeDatesRangeDateRule, () => new EditableExcludeDatesRangeRuleViewModel()),
                new AvailableRule<IEditableAutomaticDateSchedulingRuleViewModel>(MyClubResources.IntervalDateRule, () => new EditableDateIntervalRuleViewModel(), () =>  !Rules.OfType<EditableDateIntervalRuleViewModel>().Any()),
           ]);

        public EditableAutomaticDateSchedulingRulesViewModel(IEnumerable<AvailableRule<IEditableAutomaticDateSchedulingRuleViewModel>> availableRules) => AvailableRules.AddRange(availableRules);

        internal void Load(IEnumerable<IDateSchedulingRule> rules)
            => Rules.Set(rules.Select(x => x switch
            {
                IncludeDaysOfWeekRule dayOfWeeksRule => (IEditableAutomaticDateSchedulingRuleViewModel)new EditableIncludeDaysOfWeekRuleViewModel()
                {
                    SelectedDays = dayOfWeeksRule.DaysOfWeek,
                },
                ExcludeDateRule excludeDateRule => new EditableExcludeDateRuleViewModel()
                {
                    Date = excludeDateRule.Date,
                },
                ExcludeDatesRangeRule excludeDatesRangeRule => new EditableExcludeDatesRangeRuleViewModel()
                {
                    StartDate = excludeDatesRangeRule.StartDate,
                    EndDate = excludeDatesRangeRule.EndDate,
                },
                DateIntervalRule dateIntervalRule => new EditableDateIntervalRuleViewModel()
                {
                    Value = dateIntervalRule.Interval.Simplify().value,
                    Unit = dateIntervalRule.Interval.Simplify().unit
                },
                _ => throw new NotImplementedException(),
            }));
    }

    internal abstract class EditableDateSchedulingRuleViewModel : EditableObject
    {
        public virtual bool CanRemove => true;

        public virtual bool CanMove => false;
    }

    internal class EditableIncludeDaysOfWeekRuleViewModel : EditableDateSchedulingRuleViewModel, IEditableAsSoonAsPossibleDateSchedulingRuleViewModel, IEditableAutomaticDateSchedulingRuleViewModel
    {
        public EditableIncludeDaysOfWeekRuleViewModel()
        {
            var firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            AllDays = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().Rotate((int)firstDayOfWeek).ToList().AsReadOnly();

            ValidationRules.Add<EditableIncludeDaysOfWeekRuleViewModel, IEnumerable?>(x => x.SelectedDays, MyClubResources.AnySelectedDaysError, x => x is not null && x.Cast<DayOfWeek>().Any());
        }

        [CanSetIsModified(false)]
        public ReadOnlyCollection<DayOfWeek> AllDays { get; private set; }

        [DoNotCheckEquality]
        public IEnumerable? SelectedDays { get; set; }

        public IAvailableDateSchedulingRule ProvideRule() => new IncludeDaysOfWeekRule(SelectedDays?.OfType<DayOfWeek>().ToList() ?? []);

        IDateSchedulingRule IEditableAutomaticDateSchedulingRuleViewModel.ProvideRule() => new IncludeDaysOfWeekRule(SelectedDays?.OfType<DayOfWeek>().ToList() ?? []);
    }

    internal class EditableExcludeDatesRangeRuleViewModel : EditableDateSchedulingRuleViewModel, IEditableAsSoonAsPossibleDateSchedulingRuleViewModel, IEditableAutomaticDateSchedulingRuleViewModel
    {
        public EditableExcludeDatesRangeRuleViewModel() => ValidationRules.Add<EditableExcludeDatesRangeRuleViewModel, DateOnly?>(x => x.EndDate, MessageResources.FieldEndDateMustBeUpperOrEqualsThanStartDateError, x => !x.HasValue || !StartDate.HasValue || x.Value > StartDate);

        [IsRequired]
        [ValidateProperty(nameof(EndDate))]
        [Display(Name = nameof(StartDate), ResourceType = typeof(MyClubResources))]
        public DateOnly? StartDate { get; set; }

        [IsRequired]
        [Display(Name = nameof(EndDate), ResourceType = typeof(MyClubResources))]
        public DateOnly? EndDate { get; set; }

        public IAvailableDateSchedulingRule ProvideRule() => new ExcludeDatesRangeRule(StartDate.GetValueOrDefault(), EndDate.GetValueOrDefault());

        IDateSchedulingRule IEditableAutomaticDateSchedulingRuleViewModel.ProvideRule() => new ExcludeDatesRangeRule(StartDate.GetValueOrDefault(), EndDate.GetValueOrDefault());
    }

    internal class EditableIncludeTimePeriodsRuleViewModel : EditableDateSchedulingRuleViewModel, IEditableAsSoonAsPossibleDateSchedulingRuleViewModel
    {
        public EditableIncludeTimePeriodsRuleViewModel()
        {
            AddPeriodCommand = CommandsManager.CreateNotNull<EditableTimePeriodViewModel>(_ => AddNewPeriod(), CanAddPeriod);
            RemovePeriodCommand = CommandsManager.CreateNotNull<EditableTimePeriodViewModel>(RemovePeriod, CanRemovePeriod);
            AddNewPeriod();
        }
        public ObservableCollection<EditableTimePeriodViewModel> TimePeriods { get; } = [];

        public ICommand AddPeriodCommand { get; }

        public ICommand RemovePeriodCommand { get; }

        private void AddNewPeriod() => TimePeriods.Add(new EditableTimePeriodViewModel());

        private bool CanAddPeriod(EditableTimePeriodViewModel period) => TimePeriods.IndexOf(period) == TimePeriods.Count - 1 && period.StartTime.HasValue && period.EndTime.HasValue;

        private void RemovePeriod(EditableTimePeriodViewModel period)
        {
            if (TimePeriods.Count > 1)
                _ = TimePeriods.Remove(period);
        }

        private bool CanRemovePeriod(EditableTimePeriodViewModel period) => TimePeriods.Count > 1;

        public IAvailableDateSchedulingRule ProvideRule()
            => new IncludeTimePeriodsRule(TimePeriods.Select(x => new TimePeriod(x.StartTime.GetValueOrDefault(), x.EndTime.GetValueOrDefault())));
    }

    internal class EditableTimePeriodViewModel : EditableObject
    {
        public EditableTimePeriodViewModel()
        {

            ValidationRules.Add<EditableTimePeriodViewModel, TimeOnly?>(x => x.EndTime, MessageResources.FieldEndTimeMustBeUpperOrEqualsThanStartTimeError, x => !x.HasValue || !StartTime.HasValue || x.Value > StartTime);
            ValidationRules.Add<EditableTimePeriodViewModel, TimeOnly?>(x => x.StartTime, MessageResources.FieldStartTimeMustBeLowerOrEqualsThanEndTimeError, x => !x.HasValue || !EndTime.HasValue || x.Value < EndTime);
        }

        [IsRequired]
        [Display(Name = nameof(StartTime), ResourceType = typeof(MyClubResources))]
        public TimeOnly? StartTime { get; set; }

        [IsRequired]
        [Display(Name = nameof(EndTime), ResourceType = typeof(MyClubResources))]
        public TimeOnly? EndTime { get; set; }

        public override bool Equals(object? obj) => ReferenceEquals(this, obj);

        public override int GetHashCode() => StartTime.GetHashCode();
    }

    internal class EditableExcludeDateRuleViewModel : EditableDateSchedulingRuleViewModel, IEditableAutomaticDateSchedulingRuleViewModel
    {
        [IsRequired]
        [Display(Name = nameof(Date), ResourceType = typeof(MyClubResources))]
        public DateOnly? Date { get; set; }

        public IDateSchedulingRule ProvideRule() => new ExcludeDateRule(Date.GetValueOrDefault());
    }

    internal class EditableDateIntervalRuleViewModel : EditableDateSchedulingRuleViewModel, IEditableAutomaticDateSchedulingRuleViewModel
    {
        [IsRequired]
        [Display(Name = nameof(Value), ResourceType = typeof(MyClubResources))]
        public int? Value { get; set; } = 1;

        [IsRequired]
        [Display(Name = nameof(Unit), ResourceType = typeof(MyClubResources))]
        public TimeUnit Unit { get; set; } = TimeUnit.Week;


        public IDateSchedulingRule ProvideRule() => new DateIntervalRule(Value.GetValueOrDefault().ToTimeSpan(Unit));
    }
}
