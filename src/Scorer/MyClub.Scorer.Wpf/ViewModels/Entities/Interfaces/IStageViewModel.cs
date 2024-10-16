// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces
{
    internal interface IStageViewModel : IIdentifiable<Guid>, INotifyPropertyChanged
    {
        string Name { get; }

        string ShortName { get; }
    }
}
