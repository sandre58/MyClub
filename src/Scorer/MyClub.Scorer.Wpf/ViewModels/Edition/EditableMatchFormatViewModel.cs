// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Domain.MatchAggregate;
using MyNet.Observable;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditableMatchFormatViewModel : EditableObject
    {
        public EditableHalfFormatViewModel RegulationTime { get; set; } = new();

        public EditableHalfFormatViewModel ExtraTime { get; set; } = new()
        {
            Number = 2,
            Duration = 15,
            HalfTimeDuration = 5
        };

        [Display(Name = nameof(ExtraTimeIsEnabled), ResourceType = typeof(MyClubResources))]
        public bool ExtraTimeIsEnabled { get; set; }

        [Display(Name = nameof(ExtraTimeIsEnabled), ResourceType = typeof(MyClubResources))]
        public bool ShootoutsIsEnabled { get; set; }

        [Display(Name = nameof(NumberOfPenaltyShootouts), ResourceType = typeof(MyClubResources))]
        public int? NumberOfPenaltyShootouts { get; set; } = 5;

        public MatchFormat Create()
            => new(RegulationTime.Create(),
                   ExtraTimeIsEnabled ? ExtraTime.Create() : null,
                   ShootoutsIsEnabled ? NumberOfPenaltyShootouts : null);

        public void Load(MatchFormat matchFormat)
        {
            RegulationTime.Load(matchFormat.RegulationTime);
            ExtraTimeIsEnabled = matchFormat.ExtraTimeIsEnabled;
            ShootoutsIsEnabled = matchFormat.ShootoutIsEnabled;

            if (matchFormat.ExtraTime is not null)
                ExtraTime.Load(matchFormat.ExtraTime);

            if (matchFormat.NumberOfPenaltyShootouts.HasValue)
                NumberOfPenaltyShootouts = matchFormat.NumberOfPenaltyShootouts;
        }

        public void Reset() => Load(MatchFormat.Default);
    }
}
