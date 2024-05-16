// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData.Binding;
using MyNet.Utilities;
using MyNet.Observable;
using MyClub.Scorer.Wpf.Services.Providers;

namespace MyClub.Scorer.Wpf.ViewModels.HomePage.DashboardContent
{
    internal class DashboardViewModel : ObservableObject
    {
        public string? Name { get; private set; }

        public byte[]? Image { get; private set; }

        public DashboardViewModel(ProjectInfoProvider projectInfoProvider)
            => Disposables.AddRange(
            [
                projectInfoProvider.WhenPropertyChanged(x => x.Name).Subscribe(x => Name = x.Value),
                projectInfoProvider.WhenPropertyChanged(x => x.Image).Subscribe(x => Image = x.Value),
            ]);
    }
}
