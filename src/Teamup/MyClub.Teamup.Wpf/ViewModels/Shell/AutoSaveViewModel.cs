// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Wpf.Settings;
using MyNet.UI.ViewModels.Workspace;
using MyNet.Utilities;
using MyNet.Utilities.IO.AutoSave;

namespace MyClub.Teamup.Wpf.ViewModels.Shell
{
    internal class AutoSaveViewModel : NavigableWorkspaceViewModel
    {
        public virtual int Interval { get => AppSettings.Default.AutoSaveInterval; set => AppSettings.Default.AutoSaveInterval = value; }

        public virtual bool IsActive { get => AppSettings.Default.IsAutoSaveEnabled; set => AppSettings.Default.IsAutoSaveEnabled = value; }

        public AutoSaveViewModel(IAutoSaveService autoSaveService)
            => Disposables.AddRange(
            [
                AppSettings.Default.WhenPropertyChanged(x => x.AutoSaveInterval).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(Interval));
                    autoSaveService.SetInterval(Interval);
                }),
                AppSettings.Default.WhenPropertyChanged(x => x.IsAutoSaveEnabled).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(IsActive));
                    if(IsActive)
                        autoSaveService.Enable();
                    else
                        autoSaveService.Disable();
                })
            ]);

        protected override string CreateTitle() => MyClubResources.AutoSave;
    }
}
