// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyNet.UI.ViewModels.Workspace;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal abstract class SettingsEditionTabViewModel : NavigableWorkspaceViewModel
    {
        public abstract void Save();

        public virtual string[] GetPropertiesAppliedAfterRestart() => [];
    }
}
