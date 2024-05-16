// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
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
    internal class OverviewTrainingSessionsViewModel : ObservableObject
    {
        private readonly ThreadSafeObservableCollection<TrainingSessionViewModel> _previousSessions = [];
        private readonly ThreadSafeObservableCollection<TrainingSessionViewModel> _nextSessions = [];

        public ReadOnlyObservableCollection<TrainingSessionViewModel> PreviousSessions { get; }

        public ReadOnlyObservableCollection<TrainingSessionViewModel> NextSessions { get; }

        public int CountItems { get; }

        public OverviewTrainingSessionsViewModel(int countItems)
            : base()
        {
            CountItems = countItems;
            PreviousSessions = new(_previousSessions);
            NextSessions = new(_nextSessions);
        }

        public void Refresh(IEnumerable<TrainingSessionViewModel> sessions)
        {
            var orderedTrainings = sessions.Where(x => !x.IsCancelled).OrderBy(x => x.EndDate).ToList();
            _previousSessions.Set(orderedTrainings.Where(x => x.EndDate < DateTime.Now).TakeLast(CountItems));
            _nextSessions.Set(orderedTrainings.Where(x => x.StartDate > DateTime.Now).Take(CountItems));
        }
    }
}
