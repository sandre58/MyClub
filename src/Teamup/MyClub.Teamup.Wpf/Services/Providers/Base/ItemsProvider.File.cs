// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MyNet.UI.Resources;
using MyNet.Utilities;
using MyNet.Utilities.Exceptions;

namespace MyClub.Teamup.Wpf.Services.Providers.Base
{
    internal abstract class ItemsFileProvider<T> : IItemsProvider<T>
    {
        private readonly Func<T, bool> _predicate;

        public string? Filename { get; private set; }

        public IEnumerable<Exception> Exceptions { get; private set; } = [];

        protected ItemsFileProvider(string? filename = null, Func<T, bool>? predicate = null)
        {
            SetFilename(filename.OrEmpty());
            _predicate = predicate ?? new Func<T, bool>(x => true);
        }

        public void SetFilename(string filename) => Filename = filename;

        public void Clear() => Exceptions = [];

        public IEnumerable<T> ProvideItems()
        {
            if (string.IsNullOrEmpty(Filename)) return [];

            if (!File.Exists(Filename))
                throw new TranslatableException(MessageResources.FileXNotFoundError.FormatWith(Filename));

            var (items, exceptions) = LoadItems(Filename);
            Exceptions = exceptions;

            return items.Where(_predicate);
        }

        protected abstract (IEnumerable<T> items, IEnumerable<Exception> exceptions) LoadItems(string filename);
    }
}
