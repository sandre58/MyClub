// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Reactive.Subjects;
using DynamicData;
using DynamicData.Binding;
using DynamicData.PLinq;
using MyClub.Teamup.Wpf.ViewModels.CompetitionPage;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.Observable.Collections.Providers;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal class RoundDetailsByCupSourceProvider : ItemChangedSourceProvider<RoundDetailsViewModel, CupViewModel>
    {
        public RoundDetailsByCupSourceProvider(MatchdayPresentationService matchdayPresentationService, MatchPresentationService matchPresentationService, Subject<CupViewModel?> cupChanged)
            : base(cupChanged, x => x.Rounds.ToObservableChangeSet().Transform(x => x switch
            {
                KnockoutViewModel knockout => new KnockoutDetailsViewModel(knockout, matchPresentationService),
                GroupStageViewModel groupStage => (RoundDetailsViewModel)new GroupStageDetailsViewModel(groupStage, matchdayPresentationService, matchPresentationService),
                _ => throw new InvalidOperationException("Round type is unknown"),
            }).DisposeMany())
        { }
    }
}
