// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace MyClub.Teamup.Plugins.Contracts.Base
{
    public interface IImportPlugin<out T> : IPlugin
    {
        IEnumerable<T> ProvideItems();

        bool IsEnabled();
    }
}
