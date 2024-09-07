// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.UI.Collections;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.SchedulingAssistant
{
    internal class EditableSchedulingMatchViewModel : EditableWrapper<MatchViewModel>, IAppointment
    {
        private readonly UiObservableCollection<SchedulingConflict> _conflicts = [];

        public EditableSchedulingMatchViewModel(MatchViewModel item) : base(item)
        {
            Conflicts = new(_conflicts);
            Reset();
        }

        public DateTime StartDate { get; private set; }

        public DateTime EndDate { get; private set; }

        public IStadiumViewModel? Stadium { get; set; }

        public ReadOnlyObservableCollection<SchedulingConflict> Conflicts { get; }

        public bool IsEnabled => Item.CanBeRescheduled;

        public void SetDate(DateTime startDate)
        {
            using (PropertyChangedSuspender.Suspend())
                EndDate = startDate.Add(Item.GetTotalTime());
            StartDate = startDate;
        }

        public void SetConflicts(IEnumerable<SchedulingConflict> conflicts) => _conflicts.Set(conflicts);

        public void Reset()
        {
            SetDate(Item.Date);
            Stadium = Item.Stadium;
            ResetIsModified();
        }
    }
}
