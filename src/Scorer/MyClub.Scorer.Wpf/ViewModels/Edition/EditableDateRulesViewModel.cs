// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using MyClub.CrossCutting.Localization;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.UI.Resources;
using MyNet.UI.ViewModels.Rules;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using MyNet.Utilities.Units;
using PropertyChanged;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditableDateRulesViewModel : EditableRulesViewModel<EditableDateRuleViewModel>
    {
        public EditableDateRulesViewModel()
        => AvailableRules.AddRange([
                new AvailableRule<EditableDateRuleViewModel>(MyClubResources.AllowDaysOfWeekDateRule, () => new DayOfWeeksRuleViewModel(), () => !Rules.OfType<DayOfWeeksRuleViewModel>().Any()),
                new AvailableRule<EditableDateRuleViewModel>(MyClubResources.ExcludeDateDateRule, () => new ExcludeDateRuleViewModel()),
                new AvailableRule<EditableDateRuleViewModel>(MyClubResources.ExcludeDatesRangeDateRule, () => new ExcludeDatesRangeRuleViewModel()),
                new AvailableRule<EditableDateRuleViewModel>(MyClubResources.IntervalDateRule, () => new DateIntervalRuleViewModel(), () =>  !Rules.OfType<DateIntervalRuleViewModel>().Any()),
           ]);
    }

    internal abstract class EditableDateRuleViewModel : EditableObject, IEditableRule
    {
        public virtual bool CanRemove => true;

        public virtual bool CanMove => false;

        public abstract bool Match(DateTime date, DateTime? previousDate);
    }

    internal class DayOfWeeksRuleViewModel : EditableDateRuleViewModel
    {
        public DayOfWeeksRuleViewModel()
        {
            var firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            AllDays = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().Rotate((int)firstDayOfWeek).ToList().AsReadOnly();

            ValidationRules.Add<DayOfWeeksRuleViewModel, IEnumerable?>(x => x.SelectedDays, MyClubResources.AnySelectedDaysError, x => x is not null && x.Cast<DayOfWeek>().Any());
        }

        [CanSetIsModified(false)]
        public ReadOnlyCollection<DayOfWeek> AllDays { get; private set; }

        [DoNotCheckEquality]
        public IEnumerable? SelectedDays { get; set; }

        public override bool Match(DateTime date, DateTime? previousDate) => SelectedDays?.OfType<DayOfWeek>().Contains(date.DayOfWeek) ?? false;
    }

    internal class ExcludeDateRuleViewModel : EditableDateRuleViewModel
    {
        [IsRequired]
        [Display(Name = nameof(Date), ResourceType = typeof(MyClubResources))]
        public DateTime? Date { get; set; }

        public override bool Match(DateTime date, DateTime? previousDate) => !Date.HasValue || date.Date != Date.Value.Date;
    }

    internal class ExcludeDatesRangeRuleViewModel : EditableDateRuleViewModel
    {
        public ExcludeDatesRangeRuleViewModel()
        {
            ValidationRules.Add<ExcludeDatesRangeRuleViewModel, DateTime?>(x => x.EndDate, MessageResources.FieldEndDateMustBeUpperOrEqualsThanStartDateError, x => !x.HasValue || !StartDate.HasValue || x.Value > StartDate);
            ValidationRules.Add<ExcludeDatesRangeRuleViewModel, DateTime?>(x => x.StartDate, MessageResources.FieldStartDateMustBeLowerOrEqualsThanEndDateError, x => !x.HasValue || !EndDate.HasValue || x.Value < EndDate);
        }

        [IsRequired]
        [Display(Name = nameof(StartDate), ResourceType = typeof(MyClubResources))]
        public DateTime? StartDate { get; set; }

        [IsRequired]
        [Display(Name = nameof(EndDate), ResourceType = typeof(MyClubResources))]
        public DateTime? EndDate { get; set; }

        public override bool Match(DateTime date, DateTime? previousDate) => !StartDate.HasValue || !EndDate.HasValue || !new Period(StartDate.Value, EndDate.Value).Contains(date);
    }

    internal class DateIntervalRuleViewModel : EditableDateRuleViewModel
    {
        [IsRequired]
        [Display(Name = nameof(Value), ResourceType = typeof(MyClubResources))]
        public int? Value { get; set; } = 1;

        [IsRequired]
        [Display(Name = nameof(Unit), ResourceType = typeof(MyClubResources))]
        public TimeUnit Unit { get; set; } = TimeUnit.Week;


        public override bool Match(DateTime date, DateTime? previousDate) => !Value.HasValue || !previousDate.HasValue || date >= previousDate.Value.Add(Value.Value, Unit);
    }
}
