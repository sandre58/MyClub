// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.Observable;
using MyNet.UI.Collections;
using MyNet.Utilities;

namespace MyClub.Teamup.Wpf.ViewModels.TrainingPage.OverviewTab
{
    internal class OverviewPerformancesViewModel : ObservableObject
    {
        private readonly int _countItems;
        private readonly UiObservableCollection<PlayerTrainingStatisticsViewModel> _bestPerformances = [];
        private readonly UiObservableCollection<PlayerTrainingStatisticsViewModel> _worstPerformances = [];

        public ReadOnlyObservableCollection<PlayerTrainingStatisticsViewModel> BestPerformances { get; }

        public ReadOnlyObservableCollection<PlayerTrainingStatisticsViewModel> WorstPerformances { get; }

        public double AverageRating { get; private set; }

        public OverviewPerformancesViewModel(int countItems)
            : base()
        {
            _countItems = countItems;
            BestPerformances = new(_bestPerformances);
            WorstPerformances = new(_worstPerformances);
        }

        public void Refresh(IEnumerable<PlayerTrainingStatisticsViewModel> playersStatistics)
        {
            AverageRating = playersStatistics.Any(x => !double.IsNaN(x.Ratings.Average)) ? playersStatistics.Where(x => !double.IsNaN(x.Ratings.Average)).Average(x => x.Ratings.Average) : 0;
            _bestPerformances.Set(playersStatistics.Where(x => !double.IsNaN(x.Ratings.Average)).OrderByDescending(x => x.Ratings.Average).Take(_countItems));
            _worstPerformances.Set(playersStatistics.Where(x => !double.IsNaN(x.Ratings.Average)).OrderBy(x => x.Ratings.Average).Take(_countItems));
        }
    }
}
