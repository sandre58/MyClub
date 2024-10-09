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
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.UI.Resources;
using MyNet.UI.ViewModels.Rules;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditableVenueSchedulingRulesViewModel : EditableRulesViewModel<EditableVenueSchedulingRuleViewModel>
    {
        private readonly ReadOnlyObservableCollection<IEditableStadiumViewModel> _stadiums;

        public EditableVenueSchedulingRulesViewModel(ReadOnlyObservableCollection<IEditableStadiumViewModel> stadiums)
        {
            _stadiums = stadiums;
            AvailableRules.AddRange([
                new AvailableRule<EditableVenueSchedulingRuleViewModel>(MyClubResources.FirstAvailableStadium, () => new EditableFirstAvailableStadiumRuleViewModel(), () => !Rules.OfType<EditableFirstAvailableStadiumRuleViewModel>().Any()),
                new AvailableRule<EditableVenueSchedulingRuleViewModel>(MyClubResources.HomeStadium, () => new EditableHomeStadiumRuleViewModel(), () => !Rules.OfType<EditableHomeStadiumRuleViewModel>().Any()),
                new AvailableRule<EditableVenueSchedulingRuleViewModel>(MyClubResources.AwayStadium, () => new EditableAwayStadiumRuleViewModel(), () => !Rules.OfType<EditableAwayStadiumRuleViewModel>().Any()),
                new AvailableRule<EditableVenueSchedulingRuleViewModel>(MyClubResources.NoStadium, () => new EditableNoStadiumRuleViewModel(), () => !Rules.OfType<EditableNoStadiumRuleViewModel>().Any()),
                new AvailableRule<EditableVenueSchedulingRuleViewModel>(MyClubResources.SchedulingForDayRule, () => new EditableStadiumOfDayRuleViewModel(stadiums)),
                new AvailableRule<EditableVenueSchedulingRuleViewModel>(MyClubResources.SchedulingForDateRule, () => new EditableStadiumOfDateRuleViewModel(stadiums)),
                new AvailableRule<EditableVenueSchedulingRuleViewModel>(MyClubResources.SchedulingForDateRangeRule, () => new EditableStadiumOfDateRangeRuleViewModel(stadiums))
           ]);
        }

        internal void Load(IEnumerable<IAvailableVenueSchedulingRule> rules)
        => Rules.Set(rules.Select(x => x switch
            {
                FirstAvailableStadiumRule firstAvailableStadiumRule => new EditableFirstAvailableStadiumRuleViewModel()
                {
                    UseRotationTime = firstAvailableStadiumRule.UseRotationTime
                },
                HomeStadiumRule => (EditableVenueSchedulingRuleViewModel)new EditableHomeStadiumRuleViewModel(),
                AwayStadiumRule => new EditableAwayStadiumRuleViewModel(),
                NoStadiumRule => new EditableNoStadiumRuleViewModel(),
                StadiumOfDayRule stadiumOfDayRule => new EditableStadiumOfDayRuleViewModel(_stadiums)
                {
                    Day = stadiumOfDayRule.Day,
                    StadiumId = stadiumOfDayRule.StadiumId,
                },
                StadiumOfDateRule stadiumOfDateRule => new EditableStadiumOfDateRuleViewModel(_stadiums)
                {
                    Date = stadiumOfDateRule.Date,
                    StadiumId = stadiumOfDateRule.StadiumId,
                },
                StadiumOfDatesRangeRule stadiumOfDateRangeRule => new EditableStadiumOfDateRangeRuleViewModel(_stadiums)
                {
                    StartDate = stadiumOfDateRangeRule.StartDate,
                    EndDate = stadiumOfDateRangeRule.EndDate,
                    StadiumId = stadiumOfDateRangeRule.StadiumId,
                },
                _ => throw new NotImplementedException(),
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
        public EditableStadiumOfDayRuleViewModel(ReadOnlyObservableCollection<IEditableStadiumViewModel> stadiums)
        {
            AllStadiums = stadiums;
            var firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            AllDays = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().Rotate((int)firstDayOfWeek).ToList().AsReadOnly();
        }

        [CanSetIsModified(false)]
        public ReadOnlyCollection<DayOfWeek> AllDays { get; private set; }

        [IsRequired]
        [Display(Name = nameof(Day), ResourceType = typeof(MyClubResources))]
        public DayOfWeek? Day { get; set; }

        public Guid? StadiumId { get; set; }

        public ReadOnlyObservableCollection<IEditableStadiumViewModel> AllStadiums { get; }

        public override IAvailableVenueSchedulingRule ProvideRule() => new StadiumOfDayRule(Day.GetValueOrDefault(), StadiumId);
    }

    internal class EditableStadiumOfDateRuleViewModel : EditableVenueSchedulingRuleViewModel
    {
        public EditableStadiumOfDateRuleViewModel(ReadOnlyObservableCollection<IEditableStadiumViewModel> stadiums) => AllStadiums = stadiums;

        [IsRequired]
        [Display(Name = nameof(Date), ResourceType = typeof(MyClubResources))]
        public DateOnly? Date { get; set; }

        public Guid? StadiumId { get; set; }

        public ReadOnlyObservableCollection<IEditableStadiumViewModel> AllStadiums { get; }

        public override IAvailableVenueSchedulingRule ProvideRule() => new StadiumOfDateRule(Date.GetValueOrDefault(), StadiumId);
    }

    internal class EditableStadiumOfDateRangeRuleViewModel : EditableVenueSchedulingRuleViewModel
    {
        public EditableStadiumOfDateRangeRuleViewModel(ReadOnlyObservableCollection<IEditableStadiumViewModel> stadiums)
        {
            AllStadiums = stadiums;

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

        public ReadOnlyObservableCollection<IEditableStadiumViewModel> AllStadiums { get; }

        public override IAvailableVenueSchedulingRule ProvideRule() => new StadiumOfDatesRangeRule(StartDate.GetValueOrDefault(), EndDate.GetValueOrDefault(), StadiumId);
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
