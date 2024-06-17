// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.DynamicData.Extensions;
using MyNet.UI.Collections;
using MyNet.UI.ViewModels.Workspace;
using MyNet.Utilities;

namespace MyClub.Teamup.Wpf.ViewModels.TrainingSessionPage
{
    internal class TrainingSessionPageOverviewViewModel : SubItemViewModel<TrainingSessionViewModel>
    {
        private readonly int _countItems;
        private readonly UiObservableCollection<TrainingAttendanceViewModel> _bestPerformances = [];
        private readonly UiObservableCollection<TrainingAttendanceViewModel> _worstPerformances = [];

        public ReadOnlyObservableCollection<TrainingAttendanceViewModel> BestPerformances { get; }

        public ReadOnlyObservableCollection<TrainingAttendanceViewModel> WorstPerformances { get; }

        public TrainingSessionPageOverviewViewModel(int countItems)
            : base()
        {
            _countItems = countItems;
            BestPerformances = new(_bestPerformances);
            WorstPerformances = new(_worstPerformances);
        }

        protected override void OnItemChanged()
        {
            base.OnItemChanged();

            if (Item is not null)
            {
                ItemSubscriptions?.AddRange(
                [
                    Item.Attendances.ToObservableChangeSet().SubscribeAll(() => RefreshStatistics())
                ]);
            }
        }

        public void RefreshStatistics()
        {
            if (Item is null) return;
            _bestPerformances.Set(Item.Attendances.Where(x => x.Rating.HasValue).OrderByDescending(x => x.Rating!.Value).Take(_countItems));
            _worstPerformances.Set(Item.Attendances.Where(x => x.Rating.HasValue).OrderBy(x => x.Rating!.Value).Take(_countItems));
        }
    }
}
