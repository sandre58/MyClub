// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.ViewModels.Workspace;
using MyClub.Scorer.Domain.Enums;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class ProjectEditionGeneralViewModel : NavigableWorkspaceViewModel
    {
        public CompetitionType Type { get; set; }

        public bool UseTeamVenues { get; set; } = true;
    }
}
