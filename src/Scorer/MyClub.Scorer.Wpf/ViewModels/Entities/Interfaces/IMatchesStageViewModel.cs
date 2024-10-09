// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces
{
    internal interface IMatchesStageViewModel : ISchedulableStageViewModel
    {
        TimeOnly MatchTime { get; }

        ReadOnlyObservableCollection<MatchViewModel> Matches { get; }

        bool CanAutomaticRescheduleVenue();

        Task AddMatchAsync();
    }
}
