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
using MyNet.Observable.Translatables;
using MyNet.UI.Resources;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using MyNet.Utilities.Units;
using PropertyChanged;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class AvailableRuleViewModel : DisplayWrapper<Type>
    {
        private readonly Func<bool> _canAdd;

        public AvailableRuleViewModel(Type item, string resourceKey, Func<bool> canAdd) : base(item, resourceKey) => _canAdd = canAdd;

        public bool CanAdd() => _canAdd.Invoke();
    }

    public interface IDateRuleViewModel
    {
        bool Match(DateTime date, DateTime? previousDate);
    }

    public interface ITimeRuleViewModel
    {
        TimeSpan? ProvideTime(DateTime date);
    }

    public class DayOfWeeksRuleViewModel : EditableObject, IDateRuleViewModel
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

        public bool Match(DateTime date, DateTime? previousDate) => SelectedDays?.OfType<DayOfWeek>().Contains(date.DayOfWeek) ?? false;
    }

    public class ExcludeDateRuleViewModel : EditableObject, IDateRuleViewModel
    {
        [IsRequired]
        [Display(Name = nameof(Date), ResourceType = typeof(MyClubResources))]
        public DateTime? Date { get; set; }

        public bool Match(DateTime date, DateTime? previousDate) => !Date.HasValue || date.Date != Date.Value.Date;
    }

    public class ExcludeDatesRangeRuleViewModel : EditableObject, IDateRuleViewModel
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

        public bool Match(DateTime date, DateTime? previousDate) => !StartDate.HasValue || !EndDate.HasValue || !new Period(StartDate.Value, EndDate.Value).Contains(date);
    }

    public class DateIntervalRuleViewModel : EditableObject, IDateRuleViewModel
    {
        [IsRequired]
        [Display(Name = nameof(Value), ResourceType = typeof(MyClubResources))]
        public int? Value { get; set; } = 1;

        [IsRequired]
        [Display(Name = nameof(Unit), ResourceType = typeof(MyClubResources))]
        public TimeUnit Unit { get; set; } = TimeUnit.Week;


        public bool Match(DateTime date, DateTime? previousDate) => !Value.HasValue || !previousDate.HasValue || date >= previousDate.Value.Add(Value.Value, Unit);
    }

    public class TimeOfDayRuleViewModel : EditableObject, ITimeRuleViewModel
    {
        public TimeOfDayRuleViewModel()
        {
            var firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            AllDays = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().Rotate((int)firstDayOfWeek).ToList().AsReadOnly();
        }

        [CanSetIsModified(false)]
        public ReadOnlyCollection<DayOfWeek> AllDays { get; private set; }

        [IsRequired]
        [Display(Name = nameof(Day), ResourceType = typeof(MyClubResources))]
        public DayOfWeek? Day { get; set; }

        [IsRequired]
        [Display(Name = nameof(Time), ResourceType = typeof(MyClubResources))]
        public TimeSpan? Time { get; set; }

        public TimeSpan? ProvideTime(DateTime date) => date.DayOfWeek == Day && Time.HasValue ? Time.Value : null;
    }

    public class TimeOfDateRuleViewModel : EditableObject, ITimeRuleViewModel
    {
        [IsRequired]
        [Display(Name = nameof(Date), ResourceType = typeof(MyClubResources))]
        public DateTime? Date { get; set; }

        [IsRequired]
        [Display(Name = nameof(Time), ResourceType = typeof(MyClubResources))]
        public TimeSpan? Time { get; set; }

        public TimeSpan? ProvideTime(DateTime date) => Date.HasValue && date == Date.Value && Time.HasValue ? Time.Value : null;
    }

    public class TimeOfDateRangeRuleViewModel : EditableObject, ITimeRuleViewModel
    {
        public TimeOfDateRangeRuleViewModel()
        {
            ValidationRules.Add<TimeOfDateRangeRuleViewModel, DateTime?>(x => x.EndDate, MessageResources.FieldEndDateMustBeUpperOrEqualsThanStartDateError, x => !x.HasValue || !StartDate.HasValue || x.Value > StartDate);
            ValidationRules.Add<TimeOfDateRangeRuleViewModel, DateTime?>(x => x.StartDate, MessageResources.FieldStartDateMustBeLowerOrEqualsThanEndDateError, x => !x.HasValue || !EndDate.HasValue || x.Value < EndDate);
        }

        [IsRequired]
        [Display(Name = nameof(StartDate), ResourceType = typeof(MyClubResources))]
        public DateTime? StartDate { get; set; }

        [IsRequired]
        [Display(Name = nameof(EndDate), ResourceType = typeof(MyClubResources))]
        public DateTime? EndDate { get; set; }

        [IsRequired]
        [Display(Name = nameof(Time), ResourceType = typeof(MyClubResources))]
        public TimeSpan? Time { get; set; }

        public TimeSpan? ProvideTime(DateTime date) => StartDate.HasValue && EndDate.HasValue && new Period(StartDate.Value, EndDate.Value).Contains(date) && Time.HasValue ? Time.Value : null;
    }
}
