// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces
{
    internal interface IMatchdaysStageViewModel : IStageViewModel
    {
        ReadOnlyObservableCollection<MatchdayViewModel> Matchdays { get; }

        bool CanAutomaticReschedule();

        bool CanAutomaticRescheduleVenue();

        bool UseHomeVenue();

        TimeOnly ProvideStartTime();

        DateOnly ProvideStartDate();

        DateOnly ProvideEndDate();

        IEnumerable<IVirtualTeamViewModel> GetAvailableTeams();
    }
}
