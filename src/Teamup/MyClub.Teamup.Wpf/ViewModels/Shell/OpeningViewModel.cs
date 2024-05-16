// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData.Binding;
using MyNet.UI.ViewModels.Workspace;
using MyNet.Utilities;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Wpf.Settings;

namespace MyClub.Teamup.Wpf.ViewModels.Shell
{
    internal class OpeningViewModel : NavigableWorkspaceViewModel
    {
        public virtual bool OpenLastFile { get => AppSettings.Default.OpenLastProjectOnStart; set => AppSettings.Default.OpenLastProjectOnStart = value; }

        public virtual bool CheckMailConnection { get => AppSettings.Default.CheckMailConnectionOnStart; set => AppSettings.Default.CheckMailConnectionOnStart = value; }

        public virtual bool CheckDatabaseConnection { get => AppSettings.Default.CheckDatabaseConnectionOnStart; set => AppSettings.Default.CheckDatabaseConnectionOnStart = value; }

        public OpeningViewModel()
            => Disposables.AddRange([
                AppSettings.Default.WhenPropertyChanged(x => x.OpenLastProjectOnStart).Subscribe(_ => RaisePropertyChanged(nameof(OpenLastFile))),
                AppSettings.Default.WhenPropertyChanged(x => x.CheckMailConnectionOnStart).Subscribe(_ => RaisePropertyChanged(nameof(CheckMailConnection))),
                AppSettings.Default.WhenPropertyChanged(x => x.CheckDatabaseConnectionOnStart).Subscribe(_ => RaisePropertyChanged(nameof(CheckDatabaseConnection))),
        ]);

        protected override string CreateTitle() => MyClubResources.OpeningOptions;
    }
}
