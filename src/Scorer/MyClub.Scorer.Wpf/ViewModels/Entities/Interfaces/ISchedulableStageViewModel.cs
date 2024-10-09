// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;

namespace MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces
{
    internal interface ISchedulableStageViewModel : IStageViewModel
    {
        DateTime Date { get; }

        bool IsPostponed { get; }

        bool CanBePostponed();

        bool CanAutomaticReschedule();
    }
}
