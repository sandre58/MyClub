// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Observable;
using MyNet.Observable.Collections;
using MyNet.Observable.Translatables;
using MyNet.Utilities;
using PropertyChanged;

namespace MyClub.Scorer.Wpf.ViewModels.SchedulingAssistant
{
    internal class EditableSchedulingMatchViewModel : EditableWrapper<MatchViewModel>, IAppointment
    {
        private readonly ThreadSafeObservableCollection<SchedulingConflict> _conflicts = [];

        public EditableSchedulingMatchViewModel(MatchViewModel item) : base(item)
        {
            Conflicts = new(_conflicts);
            Reset();
        }

        public DateTime StartDate { get; private set; }

        public DateTime EndDate { get; private set; }

        public StadiumViewModel? Stadium { get; set; }

        public ReadOnlyObservableCollection<SchedulingConflict> Conflicts { get; }

        public void SetDate(DateTime startDate)
        {
            using (PropertyChangedSuspender.Suspend())
                EndDate = startDate.Add(Item.GetTotalTime());
            StartDate = startDate;
        }

        public void SetConflicts(IEnumerable<SchedulingConflict> conflicts) => _conflicts.Set(conflicts);

        public void Reset()
        {
            SetDate(Item.StartDate);
            Stadium = Item.Stadium;
            ResetIsModified();
        }
    }
}
