// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces
{
    internal interface ICompetitionViewModel : IIdentifiable<Guid>, IDisposable
    {
        IObservable<IChangeSet<MatchViewModel, Guid>> ProvideMatches();

        IObservable<IChangeSet<IMatchParent, Guid>> ProvideMatchParents();
    }
}
