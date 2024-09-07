// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyNet.Observable;
using MyNet.Utilities.Localization;

namespace MyClub.Scorer.Wpf.ViewModels.BuildAssistant
{
    internal class DatesSchedulingAsSoonAsPossibleViewModel : EditableObject, IDatesSchedulingMethodViewModel
    {
        public EditableAsSoonAsPossibleDateSchedulingRulesViewModel Rules { get; } = new();

        public EditableDateTime Start { get; set; } = new();

        public void Reset(DateTime startDate)
        {
            Start.Load(startDate);
            Rules.Rules.Clear();
        }

        public void Load(SchedulingParameters schedulingParameters)
        {
            Start.Load(schedulingParameters.Start());
            Rules.Load(schedulingParameters.AsSoonAsPossibleRules.SelectMany(x => x.ConvertToTimeZone(TimeZoneInfo.Utc, GlobalizationService.Current.TimeZone)));
        }

        public BuildDatesParametersDto ProvideBuildDatesParameters(int countMatchdays, int countMatchesByMatchday, TimeOnly defaultTime) => new BuildAsSoonAsPossibleDatesParametersDto
        {
            StartDate = Start.ToUtcOrDefault(),
            Rules = Rules.Rules.Select(x => x.ProvideRule()).ToList()
        };
    }
}
