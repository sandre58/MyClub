// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System;
using MyClub.CrossCutting.Localization;
using MyNet.Observable.Attributes;
using MyNet.Observable;
using MyNet.UI.Resources;
using System.Linq;
using MyNet.Utilities.DateTimes;
using MyNet.Utilities;
using MyNet.UI.ViewModels.Rules;
using MyNet.UI.Commands;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditableTimeRulesViewModel : EditableRulesViewModel<EditableTimeRuleViewModel>
    {
        public EditableTimeRulesViewModel()
            => AvailableRules.AddRange([
                new AvailableRule<EditableTimeRuleViewModel>(MyClubResources.TimeOfDayTimeRule, () => new TimeOfDayRuleViewModel()),
                new AvailableRule<EditableTimeRuleViewModel>(MyClubResources.TimeOfDateTimeRule, () => new TimeOfDateRuleViewModel()),
                new AvailableRule<EditableTimeRuleViewModel>(MyClubResources.TimeOfDateRangeTimeRule, () => new TimeOfDateRangeRuleViewModel()),
                new AvailableRule<EditableTimeRuleViewModel>(MyClubResources.TimeOfMatchNumberTimeRule, () => new TimeOfMatchNumberRuleViewModel())
           ]);
    }

    internal abstract class EditableTimeRuleViewModel : EditableObject, IEditableRule
    {
        public virtual bool CanRemove => true;

        public virtual bool CanMove => true;

        public abstract TimeSpan? ProvideTime(DateTime date, int matchIndex);
    }

    internal class TimeOfDayRuleViewModel : EditableTimeRuleViewModel
    {
        public TimeOfDayRuleViewModel()
        {
            var firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            AllDays = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().Rotate((int)firstDayOfWeek).ToList().AsReadOnly();

            AddExceptionCommand = CommandsManager.Create(() => MatchExceptions.Add(new TimeOfMatchNumberRuleViewModel()));
            RemoveExceptionCommand = CommandsManager.CreateNotNull<TimeOfMatchNumberRuleViewModel>(x => MatchExceptions.Remove(x));
        }

        public ObservableCollection<TimeOfMatchNumberRuleViewModel> MatchExceptions { get; } = [];

        [CanSetIsModified(false)]
        public ReadOnlyCollection<DayOfWeek> AllDays { get; private set; }

        [IsRequired]
        [Display(Name = nameof(Day), ResourceType = typeof(MyClubResources))]
        public DayOfWeek? Day { get; set; }

        [IsRequired]
        [Display(Name = nameof(Time), ResourceType = typeof(MyClubResources))]
        public TimeSpan? Time { get; set; }

        public ICommand AddExceptionCommand { get; }

        public ICommand RemoveExceptionCommand { get; }

        public override TimeSpan? ProvideTime(DateTime date, int matchIndex)
            => date.DayOfWeek == Day && Time.HasValue ? (MatchExceptions.Select(x => x.ProvideTime(date, matchIndex)).FirstOrDefault(x => x is not null) is TimeSpan time ? time : Time.Value) : null;
    }

    internal class TimeOfDateRuleViewModel : EditableTimeRuleViewModel
    {
        public TimeOfDateRuleViewModel()
        {
            AddExceptionCommand = CommandsManager.Create(() => MatchExceptions.Add(new TimeOfMatchNumberRuleViewModel()));
            RemoveExceptionCommand = CommandsManager.CreateNotNull<TimeOfMatchNumberRuleViewModel>(x => MatchExceptions.Remove(x));
        }

        [IsRequired]
        [Display(Name = nameof(Date), ResourceType = typeof(MyClubResources))]
        public DateTime? Date { get; set; }

        [IsRequired]
        [Display(Name = nameof(Time), ResourceType = typeof(MyClubResources))]
        public TimeSpan? Time { get; set; }

        public ObservableCollection<TimeOfMatchNumberRuleViewModel> MatchExceptions { get; } = [];

        public ICommand AddExceptionCommand { get; }

        public ICommand RemoveExceptionCommand { get; }

        public override TimeSpan? ProvideTime(DateTime date, int matchIndex)
            => Date.HasValue && date == Date.Value && Time.HasValue ? (MatchExceptions.Select(x => x.ProvideTime(date, matchIndex)).FirstOrDefault(x => x is not null) is TimeSpan time ? time : Time.Value) : null;
    }

    internal class TimeOfMatchNumberRuleViewModel : EditableTimeRuleViewModel
    {
        [IsRequired]
        [Display(Name = nameof(MatchNumber), ResourceType = typeof(MyClubResources))]
        public int? MatchNumber { get; set; } = 1;

        [IsRequired]
        [Display(Name = nameof(Time), ResourceType = typeof(MyClubResources))]
        public TimeSpan? Time { get; set; }

        public override TimeSpan? ProvideTime(DateTime date, int matchIndex) => MatchNumber == matchIndex + 1 ? Time : null;
    }

    internal class TimeOfDateRangeRuleViewModel : EditableTimeRuleViewModel
    {
        public TimeOfDateRangeRuleViewModel()
        {
            AddExceptionCommand = CommandsManager.Create(() => MatchExceptions.Add(new TimeOfMatchNumberRuleViewModel()));
            RemoveExceptionCommand = CommandsManager.CreateNotNull<TimeOfMatchNumberRuleViewModel>(x => MatchExceptions.Remove(x));

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

        public ObservableCollection<TimeOfMatchNumberRuleViewModel> MatchExceptions { get; } = [];

        public ICommand AddExceptionCommand { get; }

        public ICommand RemoveExceptionCommand { get; }

        public override TimeSpan? ProvideTime(DateTime date, int matchIndex) => StartDate.HasValue && EndDate.HasValue && new Period(StartDate.Value, EndDate.Value).Contains(date) && Time.HasValue ? (MatchExceptions.Select(x => x.ProvideTime(date, matchIndex)).FirstOrDefault(x => x is not null) is TimeSpan time ? time : Time.Value) : null;
    }
}
