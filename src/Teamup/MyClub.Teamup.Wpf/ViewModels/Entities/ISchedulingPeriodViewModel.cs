// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Media;
using MyNet.Utilities;
using MyNet.Observable;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal interface ISchedulingPeriodViewModel : IIdentifiable<Guid>, IAppointment
    {
        string Label { get; }

        Color Color { get; }
    }
}
