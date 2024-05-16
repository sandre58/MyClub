// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.List;

namespace MyClub.Teamup.Wpf.ViewModels.MedicalCenterPage.StatisticsTab
{
    public class InjuryPlayerStatisticsListParametersProvider() : ListParametersProvider($"{nameof(PlayerInjuryStatisticsViewModel.Player)}.{nameof(PlayerViewModel.InverseName)}")
    {
        public override IDisplayViewModel ProvideDisplay()
        {
            var defaultColumns = new[] { $"{nameof(PlayerInjuryStatisticsViewModel.Player)}.{nameof(PlayerViewModel.Team)}", $"{nameof(PlayerInjuryStatisticsViewModel.Player)}.{nameof(PlayerViewModel.NaturalPosition)}", $"{nameof(PlayerInjuryStatisticsViewModel.Player)}.{nameof(PlayerViewModel.Injury)}", $"{nameof(PlayerViewModel.Injuries)}.Count", $"{nameof(PlayerInjuryStatisticsViewModel.UnaivalableDurationInDays)}.Sum", $"{nameof(PlayerInjuryStatisticsViewModel.UnaivalableDurationInDays)}.Min", $"{nameof(PlayerInjuryStatisticsViewModel.UnaivalableDurationInDays)}.Max" };
            var modeList = new DisplayModeList(defaultColumns);

            return new DisplayViewModel(new DisplayMode[] { modeList }, modeList);
        }
    }

}
