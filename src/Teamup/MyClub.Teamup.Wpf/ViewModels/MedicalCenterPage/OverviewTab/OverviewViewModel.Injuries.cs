// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.Observable;
using MyNet.UI.Collections;
using MyNet.Utilities;

namespace MyClub.Teamup.Wpf.ViewModels.MedicalCenterPage.OverviewTab
{
    internal class OverviewInjuriesViewModel : ObservableObject
    {
        private readonly UiObservableCollection<InjuryViewModel> _injuries = [];

        public ReadOnlyObservableCollection<InjuryViewModel> Injuries { get; }

        public OverviewInjuriesViewModel() => Injuries = new(_injuries);

        public void Refresh(IEnumerable<InjuryViewModel> injuries) => _injuries.Set(injuries.OrderBy(x => x.EndDate));
    }
}
