// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyClub.CrossCutting.Localization;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class EditableTeamPenaltyViewModel(EditableTeamViewModel team) : EditableObject
    {
        public EditableTeamViewModel Team { get; private set; } = team;

        [Display(Name = nameof(Points), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public int Points { get; set; } = 1;

        public override bool Equals(object? obj) => obj is EditableTeamPenaltyViewModel item && ReferenceEquals(item.Team, Team);

        public override int GetHashCode() => Team.GetHashCode();
    }
}
