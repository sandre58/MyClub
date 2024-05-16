﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using MyNet.Observable;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal abstract class ImportSourceViewModel : ObservableObject
    {
        public abstract (IEnumerable<T>, bool) LoadItems<T>() where T : ImportableViewModel;

        public abstract Task<bool> InitializeAsync();

        public virtual Task RefreshAsync() => Task.CompletedTask;

        public virtual bool IsEnabled() => true;
    }
}
