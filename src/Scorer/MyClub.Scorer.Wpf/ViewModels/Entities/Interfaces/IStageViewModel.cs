// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using MyNet.Observable;
using MyNet.UI.Commands;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces
{
    internal interface IStageViewModel : IIdentifiable<Guid>, INotifyPropertyChanged, IAppointment
    {
        string Name { get; }

        string ShortName { get; }

        SchedulingParametersViewModel SchedulingParameters { get; }

        ICommand OpenCommand { get; }

        bool CanCancelMatch();

        bool CanEditMatchFormat();

        bool CanEditMatchRules();

        IEnumerable<IVirtualTeamViewModel> GetAvailableTeams();
    }
}
