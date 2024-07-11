// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.BuildAssistant
{
    internal class BuildAsSoonAsPossibleViewModel : EditableObject, IBuildMethodViewModel
    {
        public SoonAsSoonPossibleRulesViewModel Rules { get; } = new();

        [IsRequired]
        [Display(Name = nameof(StartDate), ResourceType = typeof(MyClubResources))]
        public DateTime? StartDate { get; set; }

        [IsRequired]
        [Display(Name = nameof(StartTime), ResourceType = typeof(MyClubResources))]
        public TimeSpan? StartTime { get; set; }

        public void Reset(DateTime startDate)
        {
            Refresh(startDate);
            Rules.Rules.Clear();
        }

        public void Refresh(DateTime startDate)
        {
            StartDate = startDate.Date;
            StartTime = startDate.TimeOfDay;
        }

        public BuildDatesParametersDto ProvideBuildDatesParameters(int countMatchdays, int countMatchesByMatchday, TimeSpan defaultTime) => new BuildAsSoonAsPossibleParametersDto
        {
            StartDate = StartDate.GetValueOrDefault().ToUtcDateTime(StartTime.GetValueOrDefault()),
            Rules = Rules.Rules.Select(x => x.ProvideRule()).ToList()
        };
    }
}
