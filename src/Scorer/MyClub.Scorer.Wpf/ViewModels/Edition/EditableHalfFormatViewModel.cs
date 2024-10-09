// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Domain.MatchAggregate;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditableHalfFormatViewModel : EditableObject
    {
        [IsRequired]
        [Display(Name = nameof(Number), ResourceType = typeof(MyClubResources))]
        public int? Number { get; set; } = 2;

        [IsRequired]
        [Display(Name = nameof(Duration), ResourceType = typeof(MyClubResources))]
        public int? Duration { get; set; } = 45;

        [Display(Name = nameof(HalfTimeDuration), ResourceType = typeof(MyClubResources))]
        public int? HalfTimeDuration { get; set; } = 15;

        public int GetEffectiveTime() => Number.GetValueOrDefault() * Duration.GetValueOrDefault();

        public HalfFormat Create()
            => new(Number.GetValueOrDefault(), Duration.GetValueOrDefault().Minutes(), HalfTimeDuration?.Minutes());

        public void Load(HalfFormat halfFormat)
        {
            Number = halfFormat.Number;
            Duration = (int)halfFormat.Duration.TotalMinutes;
            HalfTimeDuration = (int?)halfFormat.HalfTimeDuration?.TotalMinutes;
        }
    }
}
