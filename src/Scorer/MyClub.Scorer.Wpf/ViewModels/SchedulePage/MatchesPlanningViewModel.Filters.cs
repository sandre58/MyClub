// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DynamicData.Binding;
using MyClub.Scorer.Wpf.Filters;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.Utilities;
using MyNet.Utilities.Comparaison;
using PropertyChanged;

namespace MyClub.Scorer.Wpf.ViewModels.SchedulePage
{
    internal enum FilterMode
    {
        None,

        Parent,

        Date,

        DateRange
    }

    internal class MatchesPlanningFiltersViewModel : SpeedFiltersViewModel
    {
        public override bool IsReadOnly => true;

        public MatchesPlanningSpeedFiltersViewModel SpeedFilters { get; }

        public CompositeFilterViewModel DateFilter { get; }

        public CompositeFilterViewModel DateRangeFilter { get; }

        public CompositeFilterViewModel ParentFilter { get; }

        public MatchesPlanningFiltersViewModel(IEnumerable<TeamViewModel> teams,
                                               IEnumerable<StadiumViewModel> stadiums,
                                               IEnumerable<DateTime> dates,
                                               IEnumerable<IMatchParent> parents)
        {
            SpeedFilters = new MatchesPlanningSpeedFiltersViewModel(teams, stadiums);
            DateFilter = new CompositeFilterViewModel(new DateFilterViewModel(nameof(MatchViewModel.DateOfDay), dates)) { IsEnabled = false };
            DateRangeFilter = new CompositeFilterViewModel(new MyNet.UI.ViewModels.List.Filtering.Filters.DateFilterViewModel(nameof(MatchViewModel.DateOfDay)) { Operator = ComplexComparableOperator.IsBetween }) { IsEnabled = false };
            ParentFilter = new CompositeFilterViewModel(new MatchParentFilterViewModel(nameof(MatchViewModel.Parent), parents)) { IsEnabled = false };

            Disposables.AddRange(
            [
                Observable.FromEventPattern<FiltersChangedEventArgs>(x => SpeedFilters.FiltersChanged += x, x => SpeedFilters.FiltersChanged -= x).Subscribe(_ => OnSubFiltersChanged()),
                DateFilter.WhenAnyPropertyChanged().Merge(ParentFilter.WhenAnyPropertyChanged()).Merge(DateRangeFilter.WhenAnyPropertyChanged().Throttle(10.Milliseconds())).Subscribe(_ => DeferOrExecute())
            ]);
        }

        [SuppressPropertyChangedWarnings]
        private void OnSubFiltersChanged()
        {
            using (Defer())
                CompositeFilters.Set(SpeedFilters);
        }

        protected override void ApplyFilters(IEnumerable<ICompositeFilterViewModel> compositeFilters) => base.ApplyFilters(compositeFilters.Concat([DateFilter, ParentFilter, DateRangeFilter]));

        public override void Clear()
        {
            using (Defer())
                SpeedFilters.ToList().ForEach(x => x.Reset());
        }

        public override void Reset()
        {
            base.Reset();
            DateFilter.Reset();
            ParentFilter.Reset();
            DateRangeFilter.Reset();
        }

        public void FilterBy(FilterMode mode)
        {
            using (Defer())
                switch (mode)
                {
                    case FilterMode.None:
                        DateRangeFilter.IsEnabled = false;
                        DateFilter.IsEnabled = false;
                        ParentFilter.IsEnabled = false;
                        break;
                    case FilterMode.Parent:
                        DateRangeFilter.IsEnabled = false;
                        DateFilter.IsEnabled = false;
                        ParentFilter.IsEnabled = true;
                        break;
                    case FilterMode.Date:
                        DateRangeFilter.IsEnabled = false;
                        DateFilter.IsEnabled = true;
                        ParentFilter.IsEnabled = false;
                        break;
                    case FilterMode.DateRange:
                        DateRangeFilter.IsEnabled = true;
                        DateFilter.IsEnabled = false;
                        ParentFilter.IsEnabled = false;
                        break;
                    default:
                        break;
                }
        }
    }
}
