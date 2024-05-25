// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.DatabaseContext.Domain;
using MyClub.DatabaseContext.Infrastructure.Data;

namespace MyClub.Plugins.Base.Database
{
    public abstract class ImportItemsPlugin<T> : IDisposable
    {
        private bool _disposedValue;

        protected ImportItemsPlugin(Func<MyClubContext> createContext) => UnitOfWork = new UnitOfWork(createContext());

        public (bool isEnabled, string name, string port) GetConnectionInfo()
        {
            var (name, port) = UnitOfWork.GetConnectionInfo();
            return (UnitOfWork.CanConnect(), name, port);
        }

        protected UnitOfWork UnitOfWork { get; }

        public bool IsEnabled() => UnitOfWork.CanConnect();

        public IEnumerable<T> ProvideItems() => LoadItems(UnitOfWork);

        public abstract IEnumerable<T> LoadItems(IUnitOfWork unitOfWork);

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    UnitOfWork.Dispose();
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
