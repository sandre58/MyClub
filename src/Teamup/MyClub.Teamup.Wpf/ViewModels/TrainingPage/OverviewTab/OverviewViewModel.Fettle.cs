﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
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
    internal class OverviewFettleViewModel : ObservableObject
    {
        private readonly UiObservableCollection<PlayerTrainingStatisticsViewModel> _bestLastPerformances = [];
        private readonly UiObservableCollection<PlayerTrainingStatisticsViewModel> _worstLastPerformances = [];

        public ReadOnlyObservableCollection<PlayerTrainingStatisticsViewModel> BestLastPerformances { get; }

        public ReadOnlyObservableCollection<PlayerTrainingStatisticsViewModel> WorstLastPerformances { get; }

        public int CountItems { get; }

        public OverviewFettleViewModel(int countItems)
            : base()
        {
            CountItems = countItems;
            BestLastPerformances = new(_bestLastPerformances);
            WorstLastPerformances = new(_worstLastPerformances);
        }

        public void Refresh(IEnumerable<PlayerTrainingStatisticsViewModel> playersStatistics)
        {
            _bestLastPerformances.Set(playersStatistics.Where(x => !double.IsNaN(x.LastRatings.Average)).OrderByDescending(x => x.LastRatings.Average).Take(CountItems));
            _worstLastPerformances.Set(playersStatistics.Where(x => !double.IsNaN(x.LastRatings.Average)).OrderBy(x => !double.IsNaN(x.LastRatings.Average)).Take(CountItems));
        }
    }
}
