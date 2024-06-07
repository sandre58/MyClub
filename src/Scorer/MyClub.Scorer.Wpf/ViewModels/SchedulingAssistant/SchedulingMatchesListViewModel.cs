// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.DynamicData.Extensions;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.Observable.Threading;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using System.Collections;

namespace MyClub.Scorer.Wpf.ViewModels.SchedulingAssistant
{
    internal class SchedulingMatchesListViewModel : WrapperListViewModel<MatchViewModel, EditableSchedulingMatchViewModel>
    {
        private readonly ReadOnlyObservableCollection<SchedulingMatchesGrouping> _groupedWrappers;

        public SchedulingMatchesListViewModel() : this(new SchedulingAssistantParametersProvider()) { }

        private SchedulingMatchesListViewModel(SchedulingAssistantParametersProvider parametersProvider)
            : base(x => new EditableSchedulingMatchViewModel(x), parametersProvider)
        {
            Disposables.AddRange(
                [
                Wrappers.ToObservableChangeSet()
                        .AutoRefresh(x => x.StartDate)
                        .GroupOn(x => x.StartDate.Date)
                        .Transform(x => new SchedulingMatchesGrouping(x))
                        .ObserveOn(Scheduler.GetUIOrCurrent())
                        .Bind(out _groupedWrappers)
                        .Subscribe()
                ]);
            parametersProvider.Connect(Collection.ConnectSource());
        }

        [CanSetIsModified(false)]
        public ReadOnlyObservableCollection<SchedulingMatchesGrouping> GroupedWrappers => _groupedWrappers;

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public IEnumerable? SelectedItems { get; set; }

        public void Load(IEnumerable<MatchViewModel> matches) => Collection.Set(matches);
    }

    [CanBeValidatedForDeclaredClassOnly(false)]
    [CanSetIsModifiedAttributeForDeclaredClassOnly(false)]
    internal class SchedulingMatchesGrouping : ListViewModel<EditableSchedulingMatchViewModel>, IGroup<EditableSchedulingMatchViewModel, DateTime>, IAppointment
    {
        public SchedulingMatchesGrouping(IGroup<EditableSchedulingMatchViewModel, DateTime> group)
            : base(group.List.Connect())
        {
            GroupKey = group.GroupKey;
            List = group.List;
            Disposables.AddRange(
                [
                Items.ToObservableChangeSet()
                     .MergeManyEx(x => x.Conflicts.ToObservableChangeSet())
                     .Subscribe(_ => RaisePropertyChanged(nameof(HasConflicts))),
                Items.ToObservableChangeSet().Subscribe(_ => RaisePropertyChanged(nameof(HasConflicts)))
                ]);
        }

        public DateTime GroupKey { get; }

        public IObservableList<EditableSchedulingMatchViewModel> List { get; }

        public DateTime StartDate => GroupKey.Date;

        public DateTime EndDate => GroupKey.EndOfDay();

        public bool HasConflicts => Items.Any(x => x.Conflicts.Any());
    }
}
