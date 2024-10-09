// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using DynamicData;
using MyClub.Scorer.Domain.MatchAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces
{
    internal interface ICompetitionViewModel : IIdentifiable<Guid>, INotifyPropertyChanged, IDisposable
    {
        IObservable<IChangeSet<MatchViewModel>> ProvideMatches();

        MatchFormat MatchFormat { get; }

        MatchRules MatchRules { get; }

        SchedulingParametersViewModel SchedulingParameters { get; }
    }
}
