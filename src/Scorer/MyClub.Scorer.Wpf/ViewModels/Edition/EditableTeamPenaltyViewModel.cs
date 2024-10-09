// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using MyClub.CrossCutting.Localization;
using MyNet.Observable;
using MyNet.Observable.Attributes;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditableTeamPenaltyViewModel(IEditableTeamViewModel team) : EditableObject
    {
        public IEditableTeamViewModel Team { get; private set; } = team;

        [Display(Name = nameof(Points), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public int Points { get; set; } = 1;

        public override bool Equals(object? obj) => obj is EditableTeamPenaltyViewModel item && ReferenceEquals(item.Team, Team);

        public override int GetHashCode() => Team.GetHashCode();
    }
}
