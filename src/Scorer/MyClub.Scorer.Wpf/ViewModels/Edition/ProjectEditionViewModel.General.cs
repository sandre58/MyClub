// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Domain.Enums;
using MyNet.Observable.Attributes;
using MyNet.UI.ViewModels.Workspace;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class ProjectEditionGeneralViewModel : NavigableWorkspaceViewModel
    {
        [IsRequired]
        [Display(Name = nameof(Type), ResourceType = typeof(MyClubResources))]
        public CompetitionType Type { get; set; }

        public bool TreatNoStadiumAsWarning { get; set; }

        public bool CanEditType { get; set; }

        protected override void ResetCore()
        {
            Type = CompetitionType.League;
            TreatNoStadiumAsWarning = true;
        }
    }
}
