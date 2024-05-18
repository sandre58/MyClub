// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Teamup.Wpf.ViewModels.Import;
using MyNet.Utilities;

namespace MyClub.Teamup.Wpf.Services.Providers.Base
{
    internal abstract class ItemsImportablesProvider<TFrom, TTo> : IItemsProvider<TTo>
        where TTo : ImportableViewModel
    {
        private readonly Func<IEnumerable<TFrom>> _provideItems;

        protected ItemsImportablesProvider(Func<IEnumerable<TFrom>> provideItems) => _provideItems = provideItems;

        public IEnumerable<TTo> ProvideItems(Func<TTo, bool> predicate) => _provideItems().Select(Convert).Where(predicate);

        public IEnumerable<TTo> ProvideItems() => _provideItems().Select(Convert);

        public abstract TTo Convert(TFrom item);

        public abstract bool CanImport();
    }
}
