// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using MyNet.Utilities;
using MyNet.Observable.Collections;
using MyNet.Observable;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.TrainingPage.OverviewTab
{
    internal class OverviewAttendancesViewModel : ObservableObject
    {
        private readonly ThreadSafeObservableCollection<PlayerTrainingStatisticsViewModel> _mostAttendances = [];
        private readonly ThreadSafeObservableCollection<PlayerTrainingStatisticsViewModel> _leastAttendances = [];

        public ReadOnlyObservableCollection<PlayerTrainingStatisticsViewModel> MostAttendances { get; }

        public ReadOnlyObservableCollection<PlayerTrainingStatisticsViewModel> LeastAttendances { get; }

        public int CountItems { get; }

        public OverviewAttendancesViewModel(int countItems)
            : base()
        {
            CountItems = countItems;
            MostAttendances = new(_mostAttendances);
            LeastAttendances = new(_leastAttendances);
        }

        public void Refresh(IEnumerable<PlayerTrainingStatisticsViewModel> playersStatistics)
        {
            _mostAttendances.Set(playersStatistics.OrderByDescending(x => x.Presents.Count).Take(CountItems));
            _leastAttendances.Set(playersStatistics.OrderByDescending(x => x.AllAbsents.Count).Take(CountItems));
        }
    }
}
