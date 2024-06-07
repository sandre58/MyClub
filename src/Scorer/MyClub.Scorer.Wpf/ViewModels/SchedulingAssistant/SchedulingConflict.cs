// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Observable.Translatables;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.SchedulingAssistant
{
    internal abstract class SchedulingConflict : TranslatableObject<string>
    {
        protected SchedulingConflict(Func<string> provideValue) : base(provideValue) { }
    }

    internal class SchedulingStadiumOccupancyConflict : SchedulingConflict
    {
        public SchedulingStadiumOccupancyConflict(StadiumViewModel stadium) : base(() => MyClubResources.ConflictsRotationTimeOfStadiumXIsNotRespectedWarning.FormatWith(stadium.Name)) { }
    }

    internal class SchedulingTeamBusyConflict : SchedulingConflict
    {
        public SchedulingTeamBusyConflict(TeamViewModel team) : base(() => MyClubResources.ConflictsTeamXIsBusyWarning.FormatWith(team.Name)) { }
    }

    internal class SchedulingTeamRestTimeNotRespectedConflict : SchedulingConflict
    {
        public SchedulingTeamRestTimeNotRespectedConflict(TeamViewModel team) : base(() => MyClubResources.ConflictsRestTimeOfTeamXIsNotRespectedWarning.FormatWith(team.Name)) { }
    }

    internal class SchedulingRotationTimeNotRespectedConflict : SchedulingConflict
    {
        public SchedulingRotationTimeNotRespectedConflict(StadiumViewModel stadium) : base(() => MyClubResources.ConflictsRestTimeOfTeamXIsNotRespectedWarning.FormatWith(stadium.Name)) { }
    }
}
