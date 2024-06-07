// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.UI.ViewModels.List;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.SchedulingAssistant
{
    internal sealed class SchedulingAssistantParametersProvider : ListParametersProvider, IDisposable
    {
        private readonly ObservableCollectionExtended<StadiumViewModel> _stadiums = [];
        private readonly ObservableCollectionExtended<TeamViewModel> _teams = [];
        private readonly CompositeDisposable _disposables = [];

        public override IFiltersViewModel ProvideFilters() => new SchedulingAssistantSpeedFiltersViewModel(_teams, _stadiums);

        internal void Connect(IObservable<IChangeSet<MatchViewModel>> observable)
            => _disposables.AddRange([
                observable.AutoRefresh(x => x.Stadium).Filter(x => x.Stadium is not null).DistinctValues(x => x.Stadium!).Sort(SortExpressionComparer<StadiumViewModel>.Ascending(x => x.Address?.City ?? x.DisplayName)).Bind(_stadiums).Subscribe(),
                observable.Transform(x => x.HomeTeam).Merge(observable.Transform(x => x.AwayTeam)).DistinctValues(x => x).Sort(SortExpressionComparer<TeamViewModel>.Ascending(x => x.Name)).Bind(_teams).Subscribe()
                ]);

        public void Dispose() => _disposables.Dispose();
    }
}
