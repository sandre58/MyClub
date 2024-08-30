// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces
{
    internal interface IMatchdayParent : IIdentifiable<Guid>, INotifyPropertyChanged
    {
        ReadOnlyObservableCollection<MatchdayViewModel> Matchdays { get; }

        SchedulingParametersViewModel SchedulingParameters { get; }

        bool CanAutomaticReschedule();

        bool CanAutomaticRescheduleVenue();

        IEnumerable<ITeamViewModel> GetAvailableTeams();
    }
}
