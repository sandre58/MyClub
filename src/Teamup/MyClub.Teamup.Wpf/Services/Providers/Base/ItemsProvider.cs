// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyNet.Utilities;

namespace MyClub.Teamup.Wpf.Services.Providers.Base
{
    internal class ItemsProvider<T> : IItemsProvider<T>
    {
        private readonly IEnumerable<T> _items;

        public ItemsProvider(IEnumerable<T> items) => _items = items;

        public IEnumerable<T> ProvideItems() => _items;
    }
}
