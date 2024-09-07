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
using System.Reactive.Subjects;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyNet.Observable.Collections.Providers;
using MyNet.UI.Collections;
using MyNet.Utilities;
using MyNet.Utilities.Deferring;
using MyNet.Utilities.Logging;

namespace MyClub.Scorer.Wpf.Services.Providers.Base
{
    internal abstract class EntitiesProviderBase<TViewModel> : IDisposable, ISourceProvider<TViewModel>
        where TViewModel : IIdentifiable<Guid>, INotifyPropertyChanged
    {
        private IProject? _currentProject;
        private readonly UiObservableCollection<TViewModel> _source = [];
        private readonly IObservable<IChangeSet<TViewModel>> _observable;
        private readonly IObservable<IChangeSet<TViewModel, Guid>> _observableById;
        private readonly Subject<bool> _onLoadSubject = new();
        private readonly Subject<bool> _onUnloadSubject = new();
        private readonly CompositeDisposable _disposables = [];
        private bool _disposedValue;
        private readonly Deferrer _reloadDeferrer;

        protected EntitiesProviderBase(ProjectInfoProvider projectInfoProvider)
        {
            Items = new(_source);
            _observable = _source.ToObservableChangeSet();
            _observableById = _source.ToObservableChangeSet(x => x.Id);
            _reloadDeferrer = new(() => _currentProject.IfNotNull(Reload));

            projectInfoProvider.WhenProjectClosing(() =>
            {
                Clear();
                _currentProject = null;
            });
            projectInfoProvider.WhenProjectLoaded(x =>
            {
                _currentProject = x;
                Reload(x);
            });
        }

        public bool IsLoading { get; set; }

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
            _onUnloadSubject.OnNext(true);
        }

        protected void Reload(IProject project)
        {
            var source = ProvideObservable(project);

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            IsLoading = true;

            SourceSubscriptions = new(
                source
                .Bind(_source)
                .DisposeMany()
                .Subscribe(x =>
                {
                    if (stopWatch.IsRunning)
                    {
                        IsLoading = false;
                        stopWatch.Stop();
                        LogManager.Debug($"{GetType().Name} : Load {x.Adds} item(s) in {stopWatch.ElapsedMilliseconds}ms");
                        _onLoadSubject.OnNext(true);
                    }
                })
            );
        }

        public virtual void WhenLoaded(Action action) => _disposables.Add(_onLoadSubject.Subscribe(_ => action.Invoke()));

        public virtual void WhenUnloaded(Action action) => _disposables.Add(_onUnloadSubject.Subscribe(_ => action.Invoke()));

        public IDisposable DeferReload()
        {
            if (!_reloadDeferrer.IsDeferred)
                Clear();
            return _reloadDeferrer.Defer();
        }

        protected abstract IObservable<IChangeSet<TViewModel, Guid>> ProvideObservable(IProject project);

        protected virtual void Cleanup()
        {
            SourceSubscriptions?.Dispose();
            _disposables.Dispose();
            _onLoadSubject.Dispose();
            _onUnloadSubject.Dispose();
        }

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
