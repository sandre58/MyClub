// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Windows.Media;
using MyNet.Utilities;
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal interface IEditableTeamViewModel : IIdentifiable<Guid>, INotifyPropertyChanged
    {
        string Name { get; }

        string ShortName { get; }

        byte[]? Logo { get; }

        Color? HomeColor { get; }

        Color? AwayColor { get; }

        Country? Country { get; }

        IEditableStadiumViewModel? Stadium { get; }
    }
}
