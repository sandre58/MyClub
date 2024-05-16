// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using MyNet.Utilities.Deferring;

namespace MyClub.Teamup.Application.Deferrers
{
    public class RefreshDeferrer : IDisposable
    {
        private readonly Subject<bool> _refreshSubject = new();
        private readonly Deferrer _deferrer;
        private bool _disposedValue;
        private readonly CompositeDisposable _disposables = [];

        public RefreshDeferrer() => _deferrer = new(() => _refreshSubject.OnNext(true));

        public void Subscribe(Action action, int throttle = 0)
        {
            IObservable<bool> obs = _refreshSubject;

            if (throttle > 0)
                obs = _refreshSubject.Throttle(TimeSpan.FromMilliseconds(throttle));
            _disposables.Add(obs.Subscribe(_ => action()));
        }

        public IDisposable Defer() => _deferrer.Defer();

        public void AskRefresh() => _deferrer.DeferOrExecute();

        public bool IsDeferred() => _deferrer.IsDeferred;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _disposables.Dispose();
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
