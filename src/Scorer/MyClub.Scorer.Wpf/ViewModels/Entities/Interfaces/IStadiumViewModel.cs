// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using MyClub.Domain.Enums;
using MyNet.Utilities;
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces
{
    internal interface IStadiumViewModel : IIdentifiable<Guid>, INotifyPropertyChanged
    {
        string Name { get; }

        string DisplayName { get; }

        Ground Ground { get; }

        Address? Address { get; }
    }
}
