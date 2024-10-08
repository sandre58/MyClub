﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.Services;
using MyNet.UI.ViewModels.Shell;
using MyNet.Utilities.IO.AutoSave;

namespace MyClub.Teamup.Wpf.ViewModels.Shell
{
    internal class PreferencesViewModel : PreferencesViewModelBase
    {
        public PreferencesViewModel(IPersistentPreferencesService preferencesService, IAutoSaveService autoSaveService)
            : base(preferencesService,
            [
                new TimeAndLanguageViewModel(),
                new OpeningViewModel(),
                new AutoSaveViewModel(autoSaveService),
                new DisplayViewModel()
            ])
        { }
    }
}
