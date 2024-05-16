// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using MyNet.Observable;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.TrainingPage.OverviewTab
{
    internal class OverviewAttendanceViewModel : ObservableObject
    {
        public int AverageAttendances { get; private set; }

        public double AveragePresents { get; private set; }

        public double AverageAbsents { get; private set; }

        public double AverageApologized { get; private set; }

        public void Refresh(IEnumerable<TrainingSessionViewModel> sessions)
        {
            var trainingsForAverages = sessions.Where(x => x.IsPerformed && x.Attendances.Any()).ToList();
            var hasAttendances = trainingsForAverages.Count != 0;
            if (hasAttendances)
            {
                AverageAttendances = (int)Math.Round(trainingsForAverages.Average(x => x.Attendances.Count), 0);
                AveragePresents = trainingsForAverages.Average(x => x.Attendances.Count(y => y.Attendance == Attendance.Present));
                AverageAbsents = trainingsForAverages.Average(x => x.Attendances.Count(y => y.Attendance == Attendance.Absent));
                AverageApologized = trainingsForAverages.Average(x => x.Attendances.Count(y => y.Attendance is not Attendance.Present and not Attendance.Absent and not Attendance.Unknown));
            }
            else
            {
                AveragePresents = 0;
                AverageAbsents = 0;
                AverageApologized = 0;
                AverageAttendances = 0;
            }
        }
    }
}
