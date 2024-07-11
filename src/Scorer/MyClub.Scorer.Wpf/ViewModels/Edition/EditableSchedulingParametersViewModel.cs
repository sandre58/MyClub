// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Utilities;
using MyNet.Utilities.Units;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditableSchedulingParametersViewModel : EditableObject
    {
        public SchedulingParameters Create()
            => new(StartDate.GetValueOrDefault().ToUniversalTime(), EndDate.GetValueOrDefault().ToUniversalTime(), StartTime.GetValueOrDefault(), RotationTimeValue.GetValueOrDefault().ToTimeSpan(RotationTimeUnit), RestTimeValue.GetValueOrDefault().ToTimeSpan(RestTimeUnit), UseTeamVenues);

        [IsRequired]
        [Display(Name = nameof(StartDate), ResourceType = typeof(MyClubResources))]
        public DateTime? StartDate { get; set; }

        [IsRequired]
        [Display(Name = nameof(EndDate), ResourceType = typeof(MyClubResources))]
        public DateTime? EndDate { get; set; }

        [IsRequired]
        [Display(Name = nameof(StartTime), ResourceType = typeof(MyClubResources))]
        public TimeSpan? StartTime { get; set; }

        [IsRequired]
        [Display(Name = "RotationTime", ResourceType = typeof(MyClubResources))]
        public int? RotationTimeValue { get; set; }

        [IsRequired]
        [Display(Name = "RotationTime", ResourceType = typeof(MyClubResources))]
        public TimeUnit RotationTimeUnit { get; set; }

        [IsRequired]
        [Display(Name = "RestTime", ResourceType = typeof(MyClubResources))]
        public int? RestTimeValue { get; set; }

        [IsRequired]
        [Display(Name = "RestTime", ResourceType = typeof(MyClubResources))]
        public TimeUnit RestTimeUnit { get; set; }

        [IsRequired]
        [Display(Name = nameof(UseTeamVenues), ResourceType = typeof(MyClubResources))]
        public bool UseTeamVenues { get; set; }

        public void Load(SchedulingParametersViewModel schedulingParameters)
        {
            StartDate = schedulingParameters.StartDate.ToLocalTime();
            EndDate = schedulingParameters.EndDate.ToLocalTime();
            StartTime = schedulingParameters.StartTime;
            UseTeamVenues = schedulingParameters.UseTeamVenues;
            (RotationTimeValue, RotationTimeUnit) = schedulingParameters.RotationTime.Simplify();
            (RestTimeValue, RestTimeUnit) = schedulingParameters.RestTime.Simplify();
        }
    }
}
