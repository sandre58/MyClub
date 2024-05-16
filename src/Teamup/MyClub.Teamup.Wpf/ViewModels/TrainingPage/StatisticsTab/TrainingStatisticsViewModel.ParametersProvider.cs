// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.List;
using MyNet.UI.ViewModels.List.Filtering;

namespace MyClub.Teamup.Wpf.ViewModels.TrainingPage.SessionsTab
{
    internal class TrainingStatisticsListParametersProvider(ProjectInfoProvider projectInfoProvider) : ListParametersProvider($"{nameof(PlayerTrainingStatisticsViewModel.Player)}.{nameof(PlayerViewModel.InverseName)}")
    {
        private readonly ProjectInfoProvider _projectInfoProvider = projectInfoProvider;

        public override IFiltersViewModel ProvideFilters() => new TrainingStatisticsSpeedFiltersViewModel(_projectInfoProvider);

        public override IDisplayViewModel ProvideDisplay()
        {
            var defaultColumns = new[] { $"{nameof(PlayerTrainingStatisticsViewModel.Player)}.{nameof(PlayerViewModel.Team)}", $"{nameof(PlayerTrainingStatisticsViewModel.Player)}.{nameof(PlayerViewModel.NaturalPosition)}", $"{nameof(PlayerTrainingStatisticsViewModel.Presents)}.Count", $"{nameof(PlayerTrainingStatisticsViewModel.Absents)}.Count", $"{nameof(PlayerTrainingStatisticsViewModel.Apologized)}.Count", $"{nameof(PlayerTrainingStatisticsViewModel.Injured)}.Count", $"{nameof(PlayerTrainingStatisticsViewModel.InHolidays)}.Count", $"{nameof(PlayerTrainingStatisticsViewModel.InSelection)}.Count", $"{nameof(PlayerTrainingStatisticsViewModel.Resting)}.Count", $"{nameof(PlayerTrainingStatisticsViewModel.Apologized)}.Count", $"{nameof(PlayerTrainingStatisticsViewModel.Ratings)}.Average", $"{nameof(PlayerTrainingStatisticsViewModel.LastRatings)}.Average" };
            var modeList = new DisplayModeList(defaultColumns);

            return new DisplayViewModel(new DisplayMode[] { modeList }, modeList);
        }
    }
}
