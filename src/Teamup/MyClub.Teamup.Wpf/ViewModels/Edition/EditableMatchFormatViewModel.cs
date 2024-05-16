// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using MyNet.Utilities;
using MyNet.Observable;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Domain.MatchAggregate;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class EditableMatchFormatViewModel : EditableObject
    {
        [Display(Name = nameof(NumberOfHalves), ResourceType = typeof(MyClubResources))]
        public int NumberOfHalves { get; set; }

        [Display(Name = nameof(DurationOfHalf), ResourceType = typeof(MyClubResources))]
        public int DurationOfHalf { get; set; }

        [Display(Name = nameof(ExtraTimeIsEnabled), ResourceType = typeof(MyClubResources))]
        public bool ExtraTimeIsEnabled { get; set; }

        [Display(Name = nameof(NumberOfExtraTimeHalves), ResourceType = typeof(MyClubResources))]
        public int? NumberOfExtraTimeHalves { get; set; }

        [Display(Name = nameof(DurationOfExtraTimeHalf), ResourceType = typeof(MyClubResources))]
        public int? DurationOfExtraTimeHalf { get; set; }

        [Display(Name = nameof(ExtraTimeIsEnabled), ResourceType = typeof(MyClubResources))]
        public bool ShootoutsIsEnabled { get; set; }

        [Display(Name = nameof(NumberOfPenaltyShootouts), ResourceType = typeof(MyClubResources))]
        public int? NumberOfPenaltyShootouts { get; set; }

        public MatchFormat Create()
            => new(new HalfFormat(NumberOfHalves, DurationOfHalf.Minutes()), ExtraTimeIsEnabled ? new HalfFormat(NumberOfExtraTimeHalves.GetValueOrDefault(), DurationOfExtraTimeHalf.GetValueOrDefault().Minutes()) : null, ShootoutsIsEnabled ? NumberOfPenaltyShootouts : null);

        public void Load(MatchFormat matchFormat)
        {
            NumberOfHalves = matchFormat.RegulationTime.Number;
            DurationOfHalf = (int)matchFormat.RegulationTime.Duration.TotalMinutes;

            NumberOfExtraTimeHalves = matchFormat.ExtraTime?.Number;
            DurationOfExtraTimeHalf = (int?)matchFormat.ExtraTime?.Duration.TotalMinutes;

            ExtraTimeIsEnabled = matchFormat.ExtraTimeIsEnabled;
            ShootoutsIsEnabled = matchFormat.ShootoutIsEnabled;

            NumberOfPenaltyShootouts = matchFormat.NumberOfPenaltyShootouts;
        }

        public void Reset() => Load(MatchFormat.Default);
    }
}
