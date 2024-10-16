// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

namespace MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces
{
    internal interface IRoundViewModel : ICompetitionStageViewModel
    {
        IRoundsStageViewModel Stage { get; }
    }
}
