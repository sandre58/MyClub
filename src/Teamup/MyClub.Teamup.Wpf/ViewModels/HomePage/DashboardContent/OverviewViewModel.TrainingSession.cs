// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.Observable;
using MyNet.UI.Collections;
using MyNet.Utilities;

namespace MyClub.Teamup.Wpf.ViewModels.HomePage.DashboardContent
{
    internal class OverviewTrainingSessionViewModel : ObservableObject
    {
        private readonly int _countItems;
        private readonly UiObservableCollection<TrainingAttendanceViewModel> _bestPerformances = [];
        private readonly UiObservableCollection<TrainingAttendanceViewModel> _worstPerformances = [];

        public TrainingSessionViewModel? Session { get; private set; }

        public ReadOnlyObservableCollection<TrainingAttendanceViewModel> BestPerformances { get; }

        public ReadOnlyObservableCollection<TrainingAttendanceViewModel> WorstPerformances { get; }

        public OverviewTrainingSessionViewModel(int countItems)
            : base()
        {
            _countItems = countItems;
            BestPerformances = new(_bestPerformances);
            WorstPerformances = new(_worstPerformances);
        }

        public void Refresh(TrainingSessionViewModel? sessions)
        {
            Session = sessions;
            var attendanceWithRatings = sessions?.Attendances.Where(x => x.Rating.HasValue).ToArray() ?? [];
            _bestPerformances.Set(attendanceWithRatings.OrderByDescending(x => x.Rating!.Value).Take(_countItems));
            _worstPerformances.Set(attendanceWithRatings.OrderBy(x => x.Rating!.Value).Take(_countItems));
        }
    }
}
