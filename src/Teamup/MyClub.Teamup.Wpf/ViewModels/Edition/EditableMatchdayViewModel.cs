// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class EditableMatchdayViewModel : EditableObject
    {
        [Display(Name = nameof(DuplicatedMatchday), ResourceType = typeof(MyClubResources))]
        public IMatchdayViewModel? DuplicatedMatchday { get; set; }

        [Display(Name = nameof(InvertTeams), ResourceType = typeof(MyClubResources))]
        public bool InvertTeams { get; set; } = true;

        [IsRequired]
        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        public string Name { get; set; } = string.Empty;

        [IsRequired]
        [Display(Name = nameof(ShortName), ResourceType = typeof(MyClubResources))]

        public string ShortName { get; set; } = string.Empty;

        [IsRequired]
        [Display(Name = nameof(Date), ResourceType = typeof(MyClubResources))]
        public virtual DateTime? Date { get; set; }

        [IsRequired]
        [Display(Name = nameof(Time), ResourceType = typeof(MyClubResources))]
        public virtual TimeSpan Time { get; set; }
    }
}
