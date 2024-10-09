// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
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

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditableAutomaticTimeSchedulingRulesViewModel : EditableRulesViewModel<EditableTimeSchedulingRuleViewModel>
    {
        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public static EditableAutomaticTimeSchedulingRulesViewModel Default
            => new([
                new AvailableRule<EditableTimeSchedulingRuleViewModel>(MyClubResources.SchedulingForDayRule, () => new EditableTimeOfDayRuleViewModel()),
                new AvailableRule<EditableTimeSchedulingRuleViewModel>(MyClubResources.SchedulingForDateRule, () => new EditableTimeOfDateRuleViewModel()),
                new AvailableRule<EditableTimeSchedulingRuleViewModel>(MyClubResources.SchedulingForDateRangeRule, () => new EditableTimeOfDateRangeRuleViewModel()),
                new AvailableRule<EditableTimeSchedulingRuleViewModel>(MyClubResources.SchedulingForMatchNumberRule, () => new EditableTimeOfMatchNumberRuleViewModel())
           ]);

        public EditableAutomaticTimeSchedulingRulesViewModel(IEnumerable<AvailableRule<EditableTimeSchedulingRuleViewModel>> availableRules) => AvailableRules.AddRange(availableRules);

        internal void Load(IEnumerable<ITimeSchedulingRule> rules)
            => Rules.Set(rules.Select(x =>
            {
                switch (x)
                {
                    case TimeOfDayRule timeOfDayRule:
                        var rule1 = new EditableTimeOfDayRuleViewModel()
                        {
                            Day = timeOfDayRule.Day,
                            Time = timeOfDayRule.Time,
                        };
                        rule1.MatchExceptions.AddRange(timeOfDayRule.Exceptions.Select(x => new EditableTimeOfMatchNumberRuleViewModel()
                        {
                            MatchNumber = x.Index,
                            Time = x.Time
                        }));
                        return rule1;

                    case TimeOfDateRule timeOfDateRule:
                        var rule2 = new EditableTimeOfDateRuleViewModel()
                        {
                            Date = timeOfDateRule.Date,
                            Time = timeOfDateRule.Time,
                        };
                        rule2.MatchExceptions.AddRange(timeOfDateRule.Exceptions.Select(x => new EditableTimeOfMatchNumberRuleViewModel()
                        {
                            MatchNumber = x.Index + 1,
                            Time = x.Time
                        }));
                        return rule2;

                    case TimeOfIndexRule timeOfMatchNumberRule:
                        return (EditableTimeSchedulingRuleViewModel)new EditableTimeOfMatchNumberRuleViewModel()
                        {
                            MatchNumber = timeOfMatchNumberRule.Index,
                            Time = timeOfMatchNumberRule.Time
                        };

                    case TimeOfDatesRangeRule timeOfDateRangeRule:
                        var rule3 = new EditableTimeOfDateRangeRuleViewModel()
                        {
                            StartDate = timeOfDateRangeRule.StartDate,
                            EndDate = timeOfDateRangeRule.EndDate,
                            Time = timeOfDateRangeRule.Time,
                        };
                        rule3.MatchExceptions.AddRange(timeOfDateRangeRule.Exceptions.Select(x => new EditableTimeOfMatchNumberRuleViewModel()
                        {
                            MatchNumber = x.Index + 1,
                            Time = x.Time
                        }));
                        return rule3;

                    default:
                        throw new NotImplementedException();
                }
            }));
    }

    internal abstract class EditableTimeSchedulingRuleViewModel : EditableObject, IEditableRule
    {
        public virtual bool CanRemove => true;

        public virtual bool CanMove => true;

        public abstract ITimeSchedulingRule ProvideRule();
    }

    internal class EditableTimeOfDayRuleViewModel : EditableTimeSchedulingRuleViewModel
    {
        public EditableTimeOfDayRuleViewModel(bool allowExceptions = true)
        {
            var firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            AllDays = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().Rotate((int)firstDayOfWeek).ToList().AsReadOnly();
            AllowExceptions = allowExceptions;

            AddExceptionCommand = CommandsManager.Create(() => MatchExceptions.Add(new EditableTimeOfMatchNumberRuleViewModel()), () => AllowExceptions);
            RemoveExceptionCommand = CommandsManager.CreateNotNull<EditableTimeOfMatchNumberRuleViewModel>(x => MatchExceptions.Remove(x), _ => AllowExceptions);
        }

        public ObservableCollection<EditableTimeOfMatchNumberRuleViewModel> MatchExceptions { get; } = [];

        [CanSetIsModified(false)]
        public ReadOnlyCollection<DayOfWeek> AllDays { get; private set; }

        [CanSetIsModified(false)]
        public bool AllowExceptions { get; private set; }

        [IsRequired]
        [Display(Name = nameof(Day), ResourceType = typeof(MyClubResources))]
        public DayOfWeek? Day { get; set; }

        [IsRequired]
        [Display(Name = nameof(Time), ResourceType = typeof(MyClubResources))]
        public TimeOnly? Time { get; set; }

        public ICommand AddExceptionCommand { get; }

        public ICommand RemoveExceptionCommand { get; }

        public override ITimeSchedulingRule ProvideRule() => new TimeOfDayRule(Day.GetValueOrDefault(), Time.GetValueOrDefault(), AllowExceptions ? MatchExceptions.Select(x => x.ProvideRule()).OfType<TimeOfIndexRule>() : []);
    }

    internal class EditableTimeOfDateRuleViewModel : EditableTimeSchedulingRuleViewModel
    {
        public EditableTimeOfDateRuleViewModel(bool allowExceptions = true)
        {
            AllowExceptions = allowExceptions;
            AddExceptionCommand = CommandsManager.Create(() => MatchExceptions.Add(new EditableTimeOfMatchNumberRuleViewModel()), () => AllowExceptions);
            RemoveExceptionCommand = CommandsManager.CreateNotNull<EditableTimeOfMatchNumberRuleViewModel>(x => MatchExceptions.Remove(x), _ => AllowExceptions);
        }

        [IsRequired]
        [Display(Name = nameof(Date), ResourceType = typeof(MyClubResources))]
        public DateOnly? Date { get; set; }

        [CanSetIsModified(false)]
        public bool AllowExceptions { get; private set; }

        [IsRequired]
        [Display(Name = nameof(Time), ResourceType = typeof(MyClubResources))]
        public TimeOnly? Time { get; set; }

        public ObservableCollection<EditableTimeOfMatchNumberRuleViewModel> MatchExceptions { get; } = [];

        public ICommand AddExceptionCommand { get; }

        public ICommand RemoveExceptionCommand { get; }

        public override ITimeSchedulingRule ProvideRule() => new TimeOfDateRule(Date.GetValueOrDefault(), Time.GetValueOrDefault(), AllowExceptions ? MatchExceptions.Select(x => x.ProvideRule()).OfType<TimeOfIndexRule>() : []);
    }

    internal class EditableTimeOfMatchNumberRuleViewModel : EditableTimeSchedulingRuleViewModel
    {
        [IsRequired]
        [Display(Name = nameof(MatchNumber), ResourceType = typeof(MyClubResources))]
        public int? MatchNumber { get; set; } = 1;

        [IsRequired]
        [Display(Name = nameof(Time), ResourceType = typeof(MyClubResources))]
        public TimeOnly? Time { get; set; }

        public override ITimeSchedulingRule ProvideRule() => new TimeOfIndexRule(MatchNumber.GetValueOrDefault() - 1, Time.GetValueOrDefault());
    }

    internal class EditableTimeOfDateRangeRuleViewModel : EditableTimeSchedulingRuleViewModel
    {
        public EditableTimeOfDateRangeRuleViewModel(bool allowExceptions = true)
        {
            AllowExceptions = allowExceptions;
            AddExceptionCommand = CommandsManager.Create(() => MatchExceptions.Add(new EditableTimeOfMatchNumberRuleViewModel()), () => AllowExceptions);
            RemoveExceptionCommand = CommandsManager.CreateNotNull<EditableTimeOfMatchNumberRuleViewModel>(x => MatchExceptions.Remove(x), _ => AllowExceptions);

            ValidationRules.Add<EditableTimeOfDateRangeRuleViewModel, DateOnly?>(x => x.EndDate, MessageResources.FieldEndDateMustBeUpperOrEqualsThanStartDateError, x => !x.HasValue || !StartDate.HasValue || x.Value > StartDate);
        }

        [CanSetIsModified(false)]
        public bool AllowExceptions { get; private set; }

        [IsRequired]
        [ValidateProperty(nameof(EndDate))]
        [Display(Name = nameof(StartDate), ResourceType = typeof(MyClubResources))]
        public DateOnly? StartDate { get; set; }

        [IsRequired]
        [Display(Name = nameof(EndDate), ResourceType = typeof(MyClubResources))]
        public DateOnly? EndDate { get; set; }

        [IsRequired]
        [Display(Name = nameof(Time), ResourceType = typeof(MyClubResources))]
        public TimeOnly? Time { get; set; }

        public ObservableCollection<EditableTimeOfMatchNumberRuleViewModel> MatchExceptions { get; } = [];

        public ICommand AddExceptionCommand { get; }

        public ICommand RemoveExceptionCommand { get; }

        public override ITimeSchedulingRule ProvideRule() => new TimeOfDatesRangeRule(StartDate.GetValueOrDefault(), EndDate.GetValueOrDefault(), Time.GetValueOrDefault(), AllowExceptions ? MatchExceptions.Select(x => x.ProvideRule()).OfType<TimeOfIndexRule>() : []);
    }
}
