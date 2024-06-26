﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.Observable;
using MyNet.UI.Collections;
using MyNet.Utilities;

namespace MyClub.Teamup.Wpf.ViewModels.HomePage.DashboardContent
{
    internal class OverviewAbsencesViewModel : ObservableObject
    {
        private readonly UiObservableCollection<AbsenceViewModel> _absences = [];

        public ReadOnlyObservableCollection<AbsenceViewModel> Absences { get; }

        public OverviewAbsencesViewModel() => Absences = new(_absences);

        public void Refresh(IEnumerable<AbsenceViewModel> injuries) => _absences.Set(injuries.OrderBy(x => x.EndDate));
    }
}
