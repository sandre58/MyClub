// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces
{
    internal interface IMatchParentViewModel : ICompetitionStageViewModel
    {
        IStageViewModel Stage { get; }

        DateTime Date { get; }

        public bool ShowName { get; }

        bool CanCancelMatch();

        bool CanAutomaticReschedule();

        bool CanAutomaticRescheduleVenue();

        IEnumerable<IVirtualTeamViewModel> GetAvailableTeams();
    }
}
