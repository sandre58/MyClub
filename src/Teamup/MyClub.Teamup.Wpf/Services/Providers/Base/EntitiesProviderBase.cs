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
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Domain;

namespace MyClub.Teamup.Wpf.Services.Providers.Base
{
    internal abstract class EntitiesProviderBase<T, TViewModel> : IDisposable, IItemsProvider<TViewModel>
        where T : IEntity
        where TViewModel : IIdentifiable<Guid>, INotifyPropertyChanged
    {
        private readonly Func<T, TViewModel> _createViewModel;
        private readonly ObservableCollectionExtended<TViewModel> _source = [];
        private readonly IObservable<IChangeSet<TViewModel>> _observable;
        private readonly IObservable<IChangeSet<TViewModel, Guid>> _observableById;
        private bool _disposedValue;

        protected EntitiesProviderBase(ProjectInfoProvider projectInfoProvider, Func<T, TViewModel> createViewModel)
        {
            _createViewModel = createViewModel;
            Items = new(_source);
            _observable = _source.ToObservableChangeSet();
            _observableById = _source.ToObservableChangeSet(x => x.Id);

            projectInfoProvider.WhenProjectChanged(Reload);

            if (!Equals(projectInfoProvider.GetCurrentProject(), CurrentProject))
                Reload(projectInfoProvider.GetCurrentProject());
        }

        public ReadOnlyObservableCollection<TViewModel> Items { get; }

        protected Project? CurrentProject { get; private set; }

        protected CompositeDisposable SourceSubscriptions { get; private set; } = [];

        public int Count => Items.Count;

        public IEnumerable<TViewModel> ProvideItems() => Items;

        public TViewModel? Get(Guid id) => _source.FirstOrDefault(x => x.Id == id);

        public TViewModel GetOrThrow(Guid id) => Get(id) ?? throw new ArgumentNullException(nameof(id));

        public IObservable<IChangeSet<TViewModel, Guid>> ConnectById() => _observableById;

        public IObservable<IChangeSet<TViewModel>> Connect() => _observable;

        protected void Reload(Project? project)
        {
            CurrentProject = project;
            SourceSubscriptions?.Dispose();

            _source.Clear();

            if (project is null) return;

            var source = ProvideObservable(project);

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            SourceSubscriptions = new(
                source
                .Transform(_createViewModel)
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

        protected abstract IObservable<IChangeSet<T, Guid>> ProvideObservable(Project project);

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
