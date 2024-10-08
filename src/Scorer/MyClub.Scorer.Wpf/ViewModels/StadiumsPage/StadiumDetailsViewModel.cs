﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Threading;
using MyNet.UI.ViewModels.Workspace;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.StadiumsPage
{
    internal class StadiumDetailsViewModel : ItemViewModel<StadiumViewModel>
    {
        private readonly ReadOnlyObservableCollection<TeamViewModel> _teams;
        private readonly Subject<StadiumViewModel?> _refreshTeamsSubject = new();

        public StadiumDetailsViewModel(ISourceProvider<TeamViewModel> teamsProvider)
        {
            var dynamicFilter = _refreshTeamsSubject.Select(x => new Func<TeamViewModel, bool>(y => x is not null && x.Id == y.Stadium?.Id));
            Disposables.AddRange(
            [
                teamsProvider.Connect().Filter(dynamicFilter)
                                       .Sort(SortExpressionComparer<TeamViewModel>.Ascending(x => x.Name))
                                       .ObserveOn(Scheduler.UI)
                                       .Bind(out _teams)
                                       .Subscribe(),
                teamsProvider.Connect().WhenPropertyChanged(x => x.Stadium).Subscribe(_ => _refreshTeamsSubject.OnNext(Item)),
            ]);
        }

        protected override void OnItemChanged()
        {
            base.OnItemChanged();
            _refreshTeamsSubject.OnNext(Item);
        }
        public ReadOnlyObservableCollection<TeamViewModel> Teams => _teams;
    }
}
