// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces
{
    internal interface IMatchParent : IIdentifiable<Guid>, INotifyPropertyChanged
    {
        DateTime Date { get; }

        TimeSpan MatchTime { get; }

        string Name { get; }

        string ShortName { get; }

        bool IsPostponed { get; }

        ReadOnlyObservableCollection<MatchViewModel> Matches { get; }

        SchedulingParametersViewModel SchedulingParameters { get; }

        bool CanBePostponed();

        bool CanCancelMatch();

        bool CanEditMatchFormat();

        IEnumerable<TeamViewModel> GetAvailableTeams();

        Task AddMatchAsync();
    }
}
