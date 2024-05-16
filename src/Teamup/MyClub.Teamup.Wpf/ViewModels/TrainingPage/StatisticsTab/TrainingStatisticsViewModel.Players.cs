// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using DynamicData.Binding;
using MyNet.UI.ViewModels.List;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.TrainingPage.SessionsTab;

namespace MyClub.Teamup.Wpf.ViewModels.TrainingPage.StatisticsTab
{
    internal class TrainingStatisticsPlayersViewModel : ListViewModel<PlayerTrainingStatisticsViewModel>
    {
        public TrainingStatisticsPlayersViewModel(ProjectInfoProvider projectInfoProvider, ReadOnlyObservableCollection<PlayerTrainingStatisticsViewModel> playerTrainingStatistics)
            : base(source: playerTrainingStatistics.ToObservableChangeSet(),
                   parametersProvider: new TrainingStatisticsListParametersProvider(projectInfoProvider))
        { }
    }
}

