// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.BuildAssistant
{
    internal class DatesSchedulingAsSoonAsPossibleViewModel : EditableObject, IDatesSchedulingMethodViewModel
    {
        public EditableAsSoonAsPossibleDateSchedulingRulesViewModel Rules { get; } = new();

        [IsRequired]
        [Display(Name = nameof(StartDate), ResourceType = typeof(MyClubResources))]
        public DateTime? StartDate { get; set; }

        [IsRequired]
        [Display(Name = nameof(StartTime), ResourceType = typeof(MyClubResources))]
        public TimeSpan? StartTime { get; set; }

        public void Reset(DateTime startDate)
        {
            StartDate = startDate.Date;
            StartTime = startDate.TimeOfDay;
            Rules.Rules.Clear();
        }

        public void Load(SchedulingParameters schedulingParameters)
        {
            StartDate = schedulingParameters.StartDate.Date;
            StartTime = schedulingParameters.StartTime;
            Rules.Load(schedulingParameters.AsSoonAsPossibleRules);
        }

        public BuildDatesParametersDto ProvideBuildDatesParameters(int countMatchdays, int countMatchesByMatchday, TimeSpan defaultTime) => new BuildAsSoonAsPossibleDatesParametersDto
        {
            StartDate = StartDate.GetValueOrDefault().ToUtcDateTime(StartTime.GetValueOrDefault()),
            Rules = Rules.Rules.Select(x => x.ProvideRule()).ToList()
        };
    }
}
