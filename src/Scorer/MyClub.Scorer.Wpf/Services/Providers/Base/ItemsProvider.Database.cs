// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyClub.DatabaseContext.Application.Services;

namespace MyClub.Scorer.Wpf.Services.Providers.Base
{
    internal abstract class ItemsDatabaseProvider<T> : IItemsProvider<T>
    {
        protected DatabaseService DatabaseService { get; }
        private readonly Func<T, bool> _predicate;

        protected ItemsDatabaseProvider(DatabaseService databaseService, Func<T, bool>? predicate = null)
        {
            DatabaseService = databaseService;
            _predicate = predicate ?? new Func<T, bool>(x => true);
        }

        public (string name, string host) GetConnectionInfo() => DatabaseService.GetConnectionInfo();

        public bool CanConnect() => DatabaseService.CanConnect();

        public abstract IEnumerable<T> LoadItems();

        public IEnumerable<T> ProvideItems() => LoadItems().Where(_predicate);
    }
}
