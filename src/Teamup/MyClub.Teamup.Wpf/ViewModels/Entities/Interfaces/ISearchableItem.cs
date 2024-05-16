// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using MyNet.Utilities;

namespace MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces
{
    internal interface ISearchableItem : INotifyPropertyChanged, IIdentifiable<Guid>
    {
        string SearchDisplayName { get; }

        string SearchText { get; }

        string SearchCategory { get; }

        void Open();
    }
}
