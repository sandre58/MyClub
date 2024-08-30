// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.UI.Commands;
using MyNet.UI.Resources;
using MyNet.UI.ViewModels.Rules;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditableVenueSchedulingRulesViewModel : EditableRulesViewModel<EditableVenueSchedulingRuleViewModel>
    {
        private readonly ReadOnlyObservableCollection<IStadiumViewModel> _stadiums;

        public EditableVenueSchedulingRulesViewModel(ReadOnlyObservableCollection<IStadiumViewModel> stadiums)
        {
            _stadiums = stadiums;
            AvailableRules.AddRange([
                new AvailableRule<EditableVenueSchedulingRuleViewModel>(MyClubResources.FirstAvailableStadium, () => new EditableFirstAvailableStadiumRuleViewModel(), () => !Rules.OfType<EditableFirstAvailableStadiumRuleViewModel>().Any()),
                new AvailableRule<EditableVenueSchedulingRuleViewModel>(MyClubResources.HomeStadium, () => new EditableHomeStadiumRuleViewModel(), () => !Rules.OfType<EditableHomeStadiumRuleViewModel>().Any()),
                new AvailableRule<EditableVenueSchedulingRuleViewModel>(MyClubResources.AwayStadium, () => new EditableAwayStadiumRuleViewModel(), () => !Rules.OfType<EditableAwayStadiumRuleViewModel>().Any()),
                new AvailableRule<EditableVenueSchedulingRuleViewModel>(MyClubResources.NoStadium, () => new EditableNoStadiumRuleViewModel(), () => !Rules.OfType<EditableNoStadiumRuleViewModel>().Any()),
                new AvailableRule<EditableVenueSchedulingRuleViewModel>(MyClubResources.SchedulingForDayRule, () => new EditableStadiumOfDayRuleViewModel(stadiums)),
                new AvailableRule<EditableVenueSchedulingRuleViewModel>(MyClubResources.SchedulingForDateRule, () => new EditableStadiumOfDateRuleViewModel(stadiums)),
                new AvailableRule<EditableVenueSchedulingRuleViewModel>(MyClubResources.SchedulingForDateRangeRule, () => new EditableStadiumOfDateRangeRuleViewModel(stadiums)),
                new AvailableRule<EditableVenueSchedulingRuleViewModel>(MyClubResources.SchedulingForMatchNumberRule, () => new EditableStadiumOfMatchNumberRuleViewModel(stadiums))
           ]);
        }

        internal void Load(IEnumerable<IAvailableVenueSchedulingRule> rules)
        => Rules.Set(rules.Select(x =>
        {
            switch (x)
            {
                case FirstAvailableStadiumRule firstAvailableStadiumRule:
                    return new EditableFirstAvailableStadiumRuleViewModel()
                    {
                        UseRotationTime = firstAvailableStadiumRule.UseRotationTime
                    };

                case HomeStadiumRule:
                    return new EditableHomeStadiumRuleViewModel();

                case AwayStadiumRule:
                    return new EditableAwayStadiumRuleViewModel();

                case NoStadiumRule:
                    return new EditableNoStadiumRuleViewModel();

                case StadiumOfDayRule stadiumOfDayRule:
                    var rule1 = new EditableStadiumOfDayRuleViewModel(_stadiums)
                    {
                        Day = stadiumOfDayRule.Day,
                        StadiumId = stadiumOfDayRule.StadiumId,
                    };
                    rule1.MatchExceptions.AddRange(stadiumOfDayRule.MatchExceptions.Select(x => new EditableStadiumOfMatchNumberRuleViewModel(_stadiums)
                    {
                        MatchNumber = x.MatchNumber,
                        StadiumId = x.StadiumId
                    }));
                    return rule1;

                case StadiumOfDateRule stadiumOfDateRule:
                    var rule2 = new EditableStadiumOfDateRuleViewModel(_stadiums)
                    {
                        Date = stadiumOfDateRule.Date,
                        StadiumId = stadiumOfDateRule.StadiumId,
                    };
                    rule2.MatchExceptions.AddRange(stadiumOfDateRule.MatchExceptions.Select(x => new EditableStadiumOfMatchNumberRuleViewModel(_stadiums)
                    {
                        MatchNumber = x.MatchNumber,
                        StadiumId = x.StadiumId
                    }));
                    return rule2;

                case StadiumOfMatchNumberRule stadiumOfMatchNumberRule:
                    return (EditableVenueSchedulingRuleViewModel)new EditableStadiumOfMatchNumberRuleViewModel(_stadiums)
                    {
                        MatchNumber = stadiumOfMatchNumberRule.MatchNumber,
                        StadiumId = stadiumOfMatchNumberRule.StadiumId
                    };

                case StadiumOfDatesRangeRule stadiumOfDateRangeRule:
                    var rule3 = new EditableStadiumOfDateRangeRuleViewModel(_stadiums)
                    {
                        StartDate = stadiumOfDateRangeRule.StartDate,
                        EndDate = stadiumOfDateRangeRule.EndDate,
                        StadiumId = stadiumOfDateRangeRule.StadiumId,
                    };
                    rule3.MatchExceptions.AddRange(stadiumOfDateRangeRule.MatchExceptions.Select(x => new EditableStadiumOfMatchNumberRuleViewModel(_stadiums)
                    {
                        MatchNumber = x.MatchNumber,
                        StadiumId = x.StadiumId
                    }));
                    return rule3;

                default:
                    throw new NotImplementedException();
            }
        }));
    }

    internal abstract class EditableVenueSchedulingRuleViewModel : EditableObject, IEditableRule
    {
        public virtual bool CanRemove => true;

        public virtual bool CanMove => true;

        public abstract IAvailableVenueSchedulingRule ProvideRule();
    }

    internal class EditableStadiumOfDayRuleViewModel : EditableVenueSchedulingRuleViewModel
    {
        public EditableStadiumOfDayRuleViewModel(ReadOnlyObservableCollection<IStadiumViewModel> stadiums)
        {
            AllStadiums = stadiums;
            var firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            AllDays = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().Rotate((int)firstDayOfWeek).ToList().AsReadOnly();

            AddExceptionCommand = CommandsManager.Create(() => MatchExceptions.Add(new EditableStadiumOfMatchNumberRuleViewModel(AllStadiums)));
            RemoveExceptionCommand = CommandsManager.CreateNotNull<EditableStadiumOfMatchNumberRuleViewModel>(x => MatchExceptions.Remove(x));
        }

        public ObservableCollection<EditableStadiumOfMatchNumberRuleViewModel> MatchExceptions { get; } = [];

        [CanSetIsModified(false)]
        public ReadOnlyCollection<DayOfWeek> AllDays { get; private set; }

        [IsRequired]
        [Display(Name = nameof(Day), ResourceType = typeof(MyClubResources))]
        public DayOfWeek? Day { get; set; }

        public Guid? StadiumId { get; set; }

        public ReadOnlyObservableCollection<IStadiumViewModel> AllStadiums { get; }

        public ICommand AddExceptionCommand { get; }

        public ICommand RemoveExceptionCommand { get; }

        public override IAvailableVenueSchedulingRule ProvideRule() => new StadiumOfDayRule(Day.GetValueOrDefault(), StadiumId, MatchExceptions.Select(x => x.ProvideRule()).OfType<StadiumOfMatchNumberRule>());
    }

    internal class EditableStadiumOfDateRuleViewModel : EditableVenueSchedulingRuleViewModel
    {
        public EditableStadiumOfDateRuleViewModel(ReadOnlyObservableCollection<IStadiumViewModel> stadiums)
        {
            AllStadiums = stadiums;
            AddExceptionCommand = CommandsManager.Create(() => MatchExceptions.Add(new EditableStadiumOfMatchNumberRuleViewModel(AllStadiums)));
            RemoveExceptionCommand = CommandsManager.CreateNotNull<EditableStadiumOfMatchNumberRuleViewModel>(x => MatchExceptions.Remove(x));
        }

        [IsRequired]
        [Display(Name = nameof(Date), ResourceType = typeof(MyClubResources))]
        public DateOnly? Date { get; set; }

        public Guid? StadiumId { get; set; }

        public ReadOnlyObservableCollection<IStadiumViewModel> AllStadiums { get; }

        public ObservableCollection<EditableStadiumOfMatchNumberRuleViewModel> MatchExceptions { get; } = [];

        public ICommand AddExceptionCommand { get; }

        public ICommand RemoveExceptionCommand { get; }

        public override IAvailableVenueSchedulingRule ProvideRule() => new StadiumOfDateRule(Date.GetValueOrDefault(), StadiumId, MatchExceptions.Select(x => x.ProvideRule()).OfType<StadiumOfMatchNumberRule>());
    }

    internal class EditableStadiumOfMatchNumberRuleViewModel : EditableVenueSchedulingRuleViewModel
    {
        public EditableStadiumOfMatchNumberRuleViewModel(ReadOnlyObservableCollection<IStadiumViewModel> stadiums) => AllStadiums = stadiums;

        [IsRequired]
        [Display(Name = nameof(MatchNumber), ResourceType = typeof(MyClubResources))]
        public int? MatchNumber { get; set; } = 1;

        public Guid? StadiumId { get; set; }

        public ReadOnlyObservableCollection<IStadiumViewModel> AllStadiums { get; }

        public override IAvailableVenueSchedulingRule ProvideRule() => new StadiumOfMatchNumberRule(MatchNumber.GetValueOrDefault(), StadiumId);
    }

    internal class EditableStadiumOfDateRangeRuleViewModel : EditableVenueSchedulingRuleViewModel
    {
        public EditableStadiumOfDateRangeRuleViewModel(ReadOnlyObservableCollection<IStadiumViewModel> stadiums)
        {
            AllStadiums = stadiums;
            AddExceptionCommand = CommandsManager.Create(() => MatchExceptions.Add(new EditableStadiumOfMatchNumberRuleViewModel(AllStadiums)));
            RemoveExceptionCommand = CommandsManager.CreateNotNull<EditableStadiumOfMatchNumberRuleViewModel>(x => MatchExceptions.Remove(x));

            ValidationRules.Add<EditableStadiumOfDateRangeRuleViewModel, DateOnly?>(x => x.EndDate, MessageResources.FieldEndDateMustBeUpperOrEqualsThanStartDateError, x => !x.HasValue || !StartDate.HasValue || x.Value > StartDate);
        }

        [IsRequired]
        [ValidateProperty(nameof(EndDate))]
        [Display(Name = nameof(StartDate), ResourceType = typeof(MyClubResources))]
        public DateOnly? StartDate { get; set; }

        [IsRequired]
        [Display(Name = nameof(EndDate), ResourceType = typeof(MyClubResources))]
        public DateOnly? EndDate { get; set; }

        public Guid? StadiumId { get; set; }

        public ReadOnlyObservableCollection<IStadiumViewModel> AllStadiums { get; }

        public ObservableCollection<EditableStadiumOfMatchNumberRuleViewModel> MatchExceptions { get; } = [];

        public ICommand AddExceptionCommand { get; }

        public ICommand RemoveExceptionCommand { get; }

        public override IAvailableVenueSchedulingRule ProvideRule() => new StadiumOfDatesRangeRule(StartDate.GetValueOrDefault(), EndDate.GetValueOrDefault(), StadiumId, MatchExceptions.Select(x => x.ProvideRule()).OfType<StadiumOfMatchNumberRule>());
    }

    internal class EditableHomeStadiumRuleViewModel : EditableVenueSchedulingRuleViewModel
    {
        public override IAvailableVenueSchedulingRule ProvideRule() => new HomeStadiumRule();
    }

    internal class EditableAwayStadiumRuleViewModel : EditableVenueSchedulingRuleViewModel
    {
        public override IAvailableVenueSchedulingRule ProvideRule() => new AwayStadiumRule();
    }

    internal class EditableNoStadiumRuleViewModel : EditableVenueSchedulingRuleViewModel
    {
        public override IAvailableVenueSchedulingRule ProvideRule() => new NoStadiumRule();
    }

    internal class EditableFirstAvailableStadiumRuleViewModel : EditableVenueSchedulingRuleViewModel
    {
        [IsRequired]
        [Display(Name = nameof(UseRotationTime), ResourceType = typeof(MyClubResources))]
        public UseRotationTime UseRotationTime { get; set; }

        public override IAvailableVenueSchedulingRule ProvideRule() => new FirstAvailableStadiumRule(UseRotationTime);
    }
}
