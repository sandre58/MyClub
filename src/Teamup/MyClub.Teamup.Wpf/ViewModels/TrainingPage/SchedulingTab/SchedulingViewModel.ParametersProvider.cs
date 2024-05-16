// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.List;

namespace MyClub.Teamup.Wpf.ViewModels.TrainingPage.SchedulingTab
{
    internal class SchedulingListParametersProvider() : ListParametersProvider(nameof(ISchedulingPeriodViewModel.StartDate))
    {
        public override IDisplayViewModel ProvideDisplay() => new DisplayViewModel().AddMode<DisplayModeList>(true).AddMode<DisplayModeYear>();
    }
}
