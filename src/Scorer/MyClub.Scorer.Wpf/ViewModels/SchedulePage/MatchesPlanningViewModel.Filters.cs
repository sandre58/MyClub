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
using MyNet.Utilities.Localization;
using PropertyChanged;

namespace MyClub.Scorer.Wpf.ViewModels.SchedulePage
{
    internal enum FilterMode
    {
        None,

        CompetitionStage,

        Date
    }

    internal class MatchesPlanningFiltersViewModel : SpeedFiltersViewModel
    {
        public override bool IsReadOnly => true;

        public MatchesPlanningSpeedFiltersViewModel SpeedFilters { get; }

        public CompositeFilterViewModel DateFilter { get; }

        public CompositeFilterViewModel CompetitionStageFilter { get; }

        public MatchesPlanningFiltersViewModel(IEnumerable<IVirtualTeamViewModel> teams,
                                               IEnumerable<StadiumViewModel> stadiums,
                                               IEnumerable<DateOnly> dates,
                                               IEnumerable<IStageViewModel> stages,
                                               SchedulingParametersViewModel schedulingParameters)
        {
            SpeedFilters = new MatchesPlanningSpeedFiltersViewModel(teams, stadiums, schedulingParameters);
            DateFilter = new CompositeFilterViewModel(new DateFilterViewModel(nameof(MatchViewModel.DateOfDay), dates)) { IsEnabled = false };
            CompetitionStageFilter = new CompositeFilterViewModel(new CompetitionStageFilterViewModel(nameof(MatchViewModel.Stage), stages)) { IsEnabled = false };

            Disposables.AddRange(
            [
                Observable.FromEventPattern<FiltersChangedEventArgs>(x => SpeedFilters.FiltersChanged += x, x => SpeedFilters.FiltersChanged -= x).Subscribe(_ => OnSubFiltersChanged()),
                DateFilter.WhenAnyPropertyChanged().Merge(CompetitionStageFilter.WhenAnyPropertyChanged()).Subscribe(_ => DeferOrExecute()),
                Observable.FromEventPattern(x => GlobalizationService.Current.TimeZoneChanged += x, x => GlobalizationService.Current.TimeZoneChanged -= x).Throttle(10.Milliseconds()).Subscribe(_ => DeferOrExecute()),
            ]);
        }

        [SuppressPropertyChangedWarnings]
        private void OnSubFiltersChanged()
        {
            using (Defer())
                CompositeFilters.Set(SpeedFilters);
        }

        protected override void ApplyFilters(IEnumerable<ICompositeFilterViewModel> compositeFilters) => base.ApplyFilters(compositeFilters.Concat([DateFilter, CompetitionStageFilter]));

        public override void Clear()
        {
            using (Defer())
                SpeedFilters.ToList().ForEach(x => x.Reset());
        }

        public override void Reset()
        {
            using (Defer())
            {
                SpeedFilters.Reset();
                base.Reset();
                ResetDefaultFilters();
            }
        }

        public void ResetDefaultFilters()
        {
            using (Defer())
            {
                using (Defer())
                {
                    DateFilter.Reset();
                    CompetitionStageFilter.Reset();
                }
            }
        }

        public void FilterBy(FilterMode mode)
        {
            using (Defer())
                switch (mode)
                {
                    case FilterMode.CompetitionStage:
                        DateFilter.IsEnabled = false;
                        CompetitionStageFilter.IsEnabled = true;
                        break;
                    case FilterMode.Date:
                        DateFilter.IsEnabled = true;
                        CompetitionStageFilter.IsEnabled = false;
                        break;
                    default:
                        DateFilter.IsEnabled = false;
                        CompetitionStageFilter.IsEnabled = false;
                        break;
                }
        }
    }
}
