// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
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
        public SchedulingStadiumOccupancyConflict(IStadiumViewModel stadium) : base(() => MyClubResources.ConflictsStadiumXIsOccupancyWarning.FormatWith(stadium.Name)) { }
    }

    internal class SchedulingTeamBusyConflict : SchedulingConflict
    {
        public SchedulingTeamBusyConflict(ITeamViewModel team) : base(() => MyClubResources.ConflictsTeamXIsBusyWarning.FormatWith(team.Name)) { }
    }

    internal class SchedulingTeamRestTimeNotRespectedConflict : SchedulingConflict
    {
        public SchedulingTeamRestTimeNotRespectedConflict(ITeamViewModel team) : base(() => MyClubResources.ConflictsRestTimeOfTeamXIsNotRespectedWarning.FormatWith(team.Name)) { }
    }

    internal class SchedulingRotationTimeNotRespectedConflict : SchedulingConflict
    {
        public SchedulingRotationTimeNotRespectedConflict(IStadiumViewModel stadium) : base(() => MyClubResources.ConflictsRotationTimeOfStadiumXIsNotRespectedWarning.FormatWith(stadium.Name)) { }
    }
}
