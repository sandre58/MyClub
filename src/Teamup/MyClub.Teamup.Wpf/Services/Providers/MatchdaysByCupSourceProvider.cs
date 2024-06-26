﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using DynamicData.Binding;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;
using MyNet.DynamicData.Extensions;
using MyNet.Observable.Collections.Providers;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal class MatchdaysByCupSourceProvider : ItemChangedSourceProvider<IMatchdayViewModel, CupViewModel>
    {
        public MatchdaysByCupSourceProvider(Subject<CupViewModel?> cupChanged)
            : base(cupChanged, x => x.Rounds.ToObservableChangeSet().Filter(x => x is IMatchdayViewModel).Transform(x => (IMatchdayViewModel)x).Merge(x.Rounds.ToObservableChangeSet().Filter(x => x is GroupStageViewModel).Transform(x => (GroupStageViewModel)x).MergeManyEx(x => x.Matchdays.ToObservableChangeSet().Transform(x => (IMatchdayViewModel)x))))
        { }
    }
}
