// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Reactive.Subjects;
using DynamicData;
using MyNet.Utilities;
using MyNet.Observable.Collections.Providers;

namespace MyClub.Teamup.Wpf.Services.Providers.Base
{
    internal class ItemChangedSourceProvider<T, TItem> : SourceProvider<T>
        where T : IIdentifiable<Guid>, INotifyPropertyChanged
    {
        private readonly IDisposable _disposable;

        public ItemChangedSourceProvider(Subject<TItem?> itemChanged, Func<TItem, IObservable<IChangeSet<T, Guid>>> getObservable)
            => _disposable = itemChanged.Subscribe(x =>
            {
                if (x is not null)
                    SetSource(getObservable.Invoke(x));
                else
                    ClearSource();
            });

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _disposable.Dispose();
        }
    }
}
