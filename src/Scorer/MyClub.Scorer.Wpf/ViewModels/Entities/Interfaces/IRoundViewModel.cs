// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;

namespace MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces
{
    internal interface IRoundViewModel : ISchedulableStageViewModel
    {
        CupViewModel Stage { get; }

        ReadOnlyObservableCollection<MatchViewModel> Matches { get; }
    }
}
