﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using MyNet.UI.Commands;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces
{
    internal interface IVirtualTeamViewModel : IIdentifiable<Guid>, INotifyPropertyChanged
    {
        string Name { get; }

        string ShortName { get; }

        ICommand OpenCommand { get; }

        TeamViewModel? ProvideTeam();
    }
}
