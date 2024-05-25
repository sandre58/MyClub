// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyClub.Teamup.Plugins.Contracts.Base
{
    public interface IImportSourcePlugin<out T> : IPlugin
    {
        object CreateView();

        Task InitializeAsync();

        void Reload();

        IEnumerable<T> ProvideItems();

        event EventHandler? ItemsLoadingRequested;
    }
}
