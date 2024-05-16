// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.List;
using MyNet.UI.ViewModels.List.Sorting;

namespace MyClub.Teamup.Wpf.ViewModels.TrainingSessionPage
{
    internal class TrainingSessionAttendancesListParametersProvider : ListParametersProvider
    {
        public override ISortingViewModel ProvideSorting()
             => new ExtendedSortingViewModel(new Dictionary<string, string>
            {
                { nameof(MyClubResources.Attendance), nameof(TrainingAttendanceViewModel.Attendance) },
                { nameof(MyClubResources.Name), "Player.InverseName" },
                { nameof(MyClubResources.Team), "Player.Team" },
                { nameof(MyClubResources.Position), "Player.NaturalPosition" },
                { nameof(MyClubResources.Rating), nameof(TrainingAttendanceViewModel.Rating) }
            },
                new[] { nameof(MyClubResources.Attendance), "Player.InverseName" });

        public override IDisplayViewModel ProvideDisplay() => new DisplayViewModel().AddMode<DisplayModeGrid>(true).AddMode<DisplayModeList>();
    }
}
