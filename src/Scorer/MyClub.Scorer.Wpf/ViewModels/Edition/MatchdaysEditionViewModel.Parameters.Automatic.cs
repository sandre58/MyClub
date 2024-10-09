// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.UI.Resources;
using MyNet.UI.ViewModels.Rules;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class MatchdaysEditionParametersAutomaticViewModel : EditableObject, IAddMatchdaysMethodViewModel
    {
        public MatchdaysEditionParametersAutomaticViewModel()
        {
            ValidationRules.Add<MatchdaysEditionParametersAutomaticViewModel, DateOnly?>(x => x.EndDate, MessageResources.FieldXIsRequiredError.FormatWith(MyClubResources.EndDate), x => !UseEndDate || x.HasValue);
            ValidationRules.Add<MatchdaysEditionParametersAutomaticViewModel, DateOnly?>(x => x.EndDate, MessageResources.FieldEndDateMustBeUpperOrEqualsThanStartDateError, x => !UseEndDate || !x.HasValue || x.Value > StartDate);
            ValidationRules.Add<MatchdaysEditionParametersAutomaticViewModel, int?>(x => x.CountMatchdays, MessageResources.FieldXIsRequiredError.FormatWith(MyClubResources.Number), x => UseEndDate || x.HasValue);
        }

        public EditableAutomaticDateSchedulingRulesViewModel DateRules { get; } = new();

        public EditableAutomaticTimeSchedulingRulesViewModel TimeRules { get; } = new([
                new AvailableRule<EditableTimeSchedulingRuleViewModel>(MyClubResources.SchedulingForDayRule, () => new EditableTimeOfDayRuleViewModel(false)),
                new AvailableRule<EditableTimeSchedulingRuleViewModel>(MyClubResources.SchedulingForDateRule, () => new EditableTimeOfDateRuleViewModel(false)),
                new AvailableRule<EditableTimeSchedulingRuleViewModel>(MyClubResources.SchedulingForDateRangeRule, () => new EditableTimeOfDateRangeRuleViewModel(false))
           ]);

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public DateOnly StartDisplayDate { get; private set; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public DateOnly EndDisplayDate { get; private set; }

        [IsRequired]
        [Display(Name = nameof(StartDate), ResourceType = typeof(MyClubResources))]
        public DateOnly? StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        public int? CountMatchdays { get; set; }

        public bool UseEndDate { get; set; }

        public void Reset(LeagueViewModel stage)
        {
            UseEndDate = false;
            StartDate = DateTime.Today.ToDate();
            EndDate = stage.SchedulingParameters.EndDate;
            StartDisplayDate = stage.SchedulingParameters.StartDate;
            EndDisplayDate = stage.SchedulingParameters.EndDate;
            CountMatchdays = 1;
            DateRules.Rules.Clear();
            TimeRules.Rules.Clear();
        }

        public AddMatchdaysDatesParametersDto ProvideDatesParameters() => new AddMatchdaysAutomaticDatesParametersDto
        {
            StartDate = StartDate.GetValueOrDefault(),
            EndDate = UseEndDate ? EndDate : null,
            Number = !UseEndDate ? CountMatchdays : null,
            DateRules = DateRules.Rules.Select(x => x.ProvideRule()).ToList(),
            TimeRules = TimeRules.Rules.Select(x => x.ProvideRule()).ToList()
        };
    }
}
