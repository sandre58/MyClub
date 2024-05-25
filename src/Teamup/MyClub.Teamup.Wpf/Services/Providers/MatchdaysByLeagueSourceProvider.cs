// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Reactive.Subjects;
using DynamicData;
using DynamicData.Binding;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable.Collections.Providers;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal class MatchdaysByLeagueSourceProvider : ItemChangedSourceProvider<IMatchdayViewModel, LeagueViewModel>
    {
        public MatchdaysByLeagueSourceProvider(Subject<LeagueViewModel?> leagueChanged)
            : base(leagueChanged, x => x.Matchdays.ToObservableChangeSet().Transform(x => (IMatchdayViewModel)x))
        { }
    }
}
