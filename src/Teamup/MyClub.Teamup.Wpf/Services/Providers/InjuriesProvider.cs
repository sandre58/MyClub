// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.DynamicData.Extensions;
using MyNet.Utilities;

namespace MyClub.Teamup.Wpf.Services.Providers
{
    internal sealed class InjuriesProvider : IDisposable
    {

        private readonly ObservableCollectionExtended<InjuryViewModel> _source = [];
        private readonly CompositeDisposable _sourceSubscriptions = [];
        private readonly IObservable<IChangeSet<InjuryViewModel>> _observable;
        private readonly IObservable<IChangeSet<InjuryViewModel, Guid>> _observableById;

        public InjuriesProvider(PlayersProvider playersProvider)
        {
            Items = new(_source);
            _observable = _source.ToObservableChangeSet();
            _observableById = _source.ToObservableChangeSet(x => x.Id);

            _sourceSubscriptions.AddRange([
                playersProvider.ConnectById().MergeManyEx(x => x.Injuries.ToObservableChangeSet(x => x.Id), y => y.Id).Bind(_source).DisposeMany().Subscribe()
            ]);
        }

        public int Count => _source.Count;

        public ReadOnlyObservableCollection<InjuryViewModel> Items { get; }

        public IObservable<IChangeSet<InjuryViewModel, Guid>> ConnectById() => _observableById;

        public IObservable<IChangeSet<InjuryViewModel>> Connect() => _observable;

        public InjuryViewModel? Get(Guid id) => _source.FirstOrDefault(x => x.Id == id);

        public InjuryViewModel GetOrThrow(Guid id) => Get(id) ?? throw new ArgumentNullException(nameof(id));

        public void Dispose() => _sourceSubscriptions.Dispose();
    }
}
