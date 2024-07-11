// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using DynamicData;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Domain.Factories;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.UI.Commands;
using MyNet.UI.Resources;
using MyNet.UI.ViewModels.Rules;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using PropertyChanged;

namespace MyClub.Scorer.Wpf.ViewModels.BuildAssistant
{
    internal class SoonAsSoonPossibleRulesViewModel : EditableRulesViewModel<AsSoonAsPossibleRuleViewModel>
    {
        public SoonAsSoonPossibleRulesViewModel()
        => AvailableRules.AddRange([
                new AvailableRule<AsSoonAsPossibleRuleViewModel>(MyClubResources.AllowTimePeriodsDateRule, () => new IncludeTimePeriodsRuleViewModel(), () => !Rules.OfType<IncludeTimePeriodsRuleViewModel>().Any()),
                new AvailableRule<AsSoonAsPossibleRuleViewModel>(MyClubResources.AllowDaysOfWeekDateRule, () => new IncludeDaysOfWeekRuleViewModel(), () => !Rules.OfType<IncludeDaysOfWeekRuleViewModel>().Any()),
                new AvailableRule<AsSoonAsPossibleRuleViewModel>(MyClubResources.ExcludeDatesRangeDateRule, () => new ExcludeDatesRangeRuleViewModel()),
           ]);
    }

    internal abstract class AsSoonAsPossibleRuleViewModel : EditableObject, IEditableRule
    {
        public virtual bool CanRemove => true;

        public virtual bool CanMove => false;

        public abstract IAvailableDateRule ProvideRule();
    }

    internal class IncludeDaysOfWeekRuleViewModel : AsSoonAsPossibleRuleViewModel
    {
        public IncludeDaysOfWeekRuleViewModel()
        {
            var firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            AllDays = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().Rotate((int)firstDayOfWeek).ToList().AsReadOnly();

            ValidationRules.Add<IncludeDaysOfWeekRuleViewModel, IEnumerable?>(x => x.SelectedDays, MyClubResources.AnySelectedDaysError, x => x is not null && x.Cast<DayOfWeek>().Any());
        }

        [CanSetIsModified(false)]
        public ReadOnlyCollection<DayOfWeek> AllDays { get; private set; }

        [DoNotCheckEquality]
        public IEnumerable? SelectedDays { get; set; }

        public override IAvailableDateRule ProvideRule() => new IncludeDaysOfWeekRule(SelectedDays?.OfType<DayOfWeek>() ?? []);
    }

    internal class ExcludeDatesRangeRuleViewModel : AsSoonAsPossibleRuleViewModel
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

        public override IAvailableDateRule ProvideRule() => new ExcludePeriodRule(new Period(StartDate.GetValueOrDefault().BeginningOfDay().ToUniversalTime(), EndDate.GetValueOrDefault().EndOfDay().ToUniversalTime()));
    }

    internal class IncludeTimePeriodsRuleViewModel : AsSoonAsPossibleRuleViewModel
    {
        public IncludeTimePeriodsRuleViewModel()
        {
            AddPeriodCommand = CommandsManager.CreateNotNull<TimePeriodViewModel>(_ => AddNewPeriod(), CanAddPeriod);
            RemovePeriodCommand = CommandsManager.CreateNotNull<TimePeriodViewModel>(RemovePeriod, CanRemovePeriod);
            AddNewPeriod();
        }
        public ObservableCollection<TimePeriodViewModel> TimePeriods { get; } = [];
        public ICommand AddPeriodCommand { get; }

        public ICommand RemovePeriodCommand { get; }

        private void AddNewPeriod() => TimePeriods.Add(new TimePeriodViewModel());

        private bool CanAddPeriod(TimePeriodViewModel period) => TimePeriods.IndexOf(period) == TimePeriods.Count - 1 && period.StartTime.HasValue && period.EndTime.HasValue;

        private void RemovePeriod(TimePeriodViewModel period)
        {
            if (TimePeriods.Count > 1)
                _ = TimePeriods.Remove(period);
        }

        private bool CanRemovePeriod(TimePeriodViewModel period) => TimePeriods.Count > 1;

        public override IAvailableDateRule ProvideRule()
            => new IncludeTimePeriodsRule(TimePeriods.Select(x => new TimePeriod(x.StartTime.GetValueOrDefault(), x.EndTime.GetValueOrDefault(), DateTimeKind.Local)));
    }

    internal class TimePeriodViewModel : EditableObject
    {
        public TimePeriodViewModel()
        {

            ValidationRules.Add<TimePeriodViewModel, TimeSpan?>(x => x.EndTime, MessageResources.FieldEndTimeMustBeUpperOrEqualsThanStartTimeError, x => !x.HasValue || !StartTime.HasValue || x.Value > StartTime);
            ValidationRules.Add<TimePeriodViewModel, TimeSpan?>(x => x.StartTime, MessageResources.FieldStartTimeMustBeLowerOrEqualsThanEndTimeError, x => !x.HasValue || !EndTime.HasValue || x.Value < EndTime);
        }

        [IsRequired]
        [Display(Name = nameof(StartTime), ResourceType = typeof(MyClubResources))]
        public TimeSpan? StartTime { get; set; }

        [IsRequired]
        [Display(Name = nameof(EndTime), ResourceType = typeof(MyClubResources))]
        public TimeSpan? EndTime { get; set; }

        public override bool Equals(object? obj) => ReferenceEquals(this, obj);

        public override int GetHashCode() => StartTime.GetHashCode();
    }
}
