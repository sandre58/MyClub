// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces
{
    internal interface ICompetitionViewModel : IDisposable, INotifyPropertyChanged
    {
        ReadOnlyObservableCollection<MatchViewModel> Matches { get; }
    }
}
