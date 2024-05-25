// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyNet.Utilities;
using MyNet.Utilities.Logging;
using MyNet.Observable.Collections;
using MyNet.Observable.Collections.Providers;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyNet.Utilities.Providers;

namespace MyClub.Scorer.Wpf.Services.Providers.Base
{
    internal abstract class EntitiesProviderBase<TViewModel> : IDisposable, ISourceProvider<TViewModel>
        where TViewModel : IIdentifiable<Guid>, INotifyPropertyChanged
    {
        private readonly ThreadSafeObservableCollection<TViewModel> _source = [];
        private readonly IObservable<IChangeSet<TViewModel>> _observable;
        private readonly IObservable<IChangeSet<TViewModel, Guid>> _observableById;
        private bool _disposedValue;

        protected EntitiesProviderBase(ProjectInfoProvider projectInfoProvider)
        {
            Items = new(_source);
            _observable = _source.ToObservableChangeSet();
            _observableById = _source.ToObservableChangeSet(x => x.Id);

            projectInfoProvider.WhenProjectClosing(Clear);
            projectInfoProvider.WhenProjectLoaded(Reload);
        }

        public ReadOnlyObservableCollection<TViewModel> Items { get; }

        ReadOnlyObservableCollection<TViewModel> ISourceProvider<TViewModel>.Source => Items;

        protected CompositeDisposable SourceSubscriptions { get; private set; } = [];

        public int Count => Items.Count;

        public IEnumerable<TViewModel> ProvideItems() => Items;

        public TViewModel? Get(Guid id) => _source.FirstOrDefault(x => x.Id == id);

        public TViewModel GetOrThrow(Guid id) => Get(id) ?? throw new ArgumentNullException(nameof(id));

        public IObservable<IChangeSet<TViewModel, Guid>> ConnectById() => _observableById;

        public IObservable<IChangeSet<TViewModel>> Connect() => _observable;

        private void Clear()
        {
            SourceSubscriptions?.Dispose();
            _source.Clear();
        }

        protected void Reload(IProject project)
        {
            var source = ProvideObservable(project);

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            SourceSubscriptions = new(
                source
                .Bind(_source)
                .DisposeMany()
                .Subscribe(x =>
                {
                    if (stopWatch.IsRunning)
                    {
                        stopWatch.Stop();
                        LogManager.Debug($"{GetType().Name} : Load {x.Adds} item(s) in {stopWatch.ElapsedMilliseconds}ms");
                    }
                })
            );
        }

        public void Reload() { }

        protected abstract IObservable<IChangeSet<TViewModel, Guid>> ProvideObservable(IProject project);

        protected virtual void Cleanup() => SourceSubscriptions?.Dispose();

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Cleanup();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
