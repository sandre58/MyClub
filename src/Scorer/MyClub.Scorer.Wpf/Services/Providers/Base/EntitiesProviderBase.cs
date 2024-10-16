// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyNet.DynamicData.Extensions;
using MyNet.Observable.Collections.Providers;
using MyNet.Observable.Deferrers;
using MyNet.Utilities;
using MyNet.Utilities.Deferring;
using MyNet.Utilities.Logging;

namespace MyClub.Scorer.Wpf.Services.Providers.Base
{
    internal abstract class EntitiesProviderBase<TViewModel> : IDisposable, ISourceProvider<TViewModel>
        where TViewModel : IIdentifiable<Guid>, INotifyPropertyChanged
    {
        private readonly ProjectInfoProvider _projectInfoProvider;
        private IProject? _currentProject;
        private readonly ExtendedObservableCollection<TViewModel> _source = [];
        private readonly IObservable<IChangeSet<TViewModel>> _observable;
        private readonly IObservable<IChangeSet<TViewModel, Guid>> _observableById;
        private readonly CompositeDisposable _disposables = [];
        private bool _disposedValue;
        private readonly Deferrer _reloadDeferrer;

        protected EntitiesProviderBase(ProjectInfoProvider projectInfoProvider)
        {
            _projectInfoProvider = projectInfoProvider;
            LoadRunner = new((x, y) =>
            {
                var source = ProvideObservable(x);

                SourceSubscriptions = new(
                    source
                    .ObserveOn(MyNet.UI.Threading.Scheduler.GetUIOrCurrent())
                    .Bind(_source)
                    .DisposeMany()
                    .Subscribe(z => LoadRunner?.IsRunning.IfTrue(() => y.OnNext(x)))
                );
            }, true);
            LoadRunner.RegisterOnEnd(this, _ => LogManager.Trace($"{GetType().Name} : Load {_source.Count} item(s) in {LoadRunner.LastTimeElapsed.Milliseconds}ms"));

            UnloadRunner = new(() =>
            {
                SourceSubscriptions?.Dispose();
                MyNet.UI.Threading.Scheduler.GetUIOrCurrent().Schedule(_ => _source.Clear());
            });

            Items = new(_source);
            _observable = _source.ToObservableChangeSet();
            _observableById = _source.ToObservableChangeSet(x => x.Id);
            _reloadDeferrer = new(() => _currentProject.IfNotNull(Reload));

            projectInfoProvider.UnloadRunner.RegisterOnStart(this, () =>
            {
                Clear();
                _currentProject = null;
            });
            projectInfoProvider.LoadRunner.RegisterOnEnd(this, x =>
            {
                _currentProject = x;

                Reload(x);
            });
        }

        public ActionRunner<IProject, IProject> LoadRunner { get; }

        public ActionRunner UnloadRunner { get; }

        public ReadOnlyObservableCollection<TViewModel> Items { get; }

        ReadOnlyObservableCollection<TViewModel> ISourceProvider<TViewModel>.Source => Items;

        protected CompositeDisposable SourceSubscriptions { get; private set; } = [];

        public int Count => Items.Count;

        public IEnumerable<TViewModel> ProvideItems() => Items;

        public virtual TViewModel? Get(Guid id) => _source.FirstOrDefault(x => x.Id == id);

        public TViewModel GetOrThrow(Guid id) => Get(id) ?? throw new ArgumentNullException(nameof(id));

        public IObservable<IChangeSet<TViewModel, Guid>> ConnectById() => _observableById;

        public IObservable<IChangeSet<TViewModel>> Connect() => _observable;

        protected virtual void Clear() => UnloadRunner.Run();

        protected void Reload(IProject project) => LoadRunner.Run(project, () => project);

        public IDisposable DeferReload()
        {
            if (!_reloadDeferrer.IsDeferred)
                Clear();
            return _reloadDeferrer.Defer();
        }

        protected abstract IObservable<IChangeSet<TViewModel>> ProvideObservable(IProject project);

        protected virtual void Cleanup()
        {
            _projectInfoProvider.LoadRunner.Unregister(this);
            _projectInfoProvider.UnloadRunner.Unregister(this);
            SourceSubscriptions?.Dispose();
            _disposables.Dispose();
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
