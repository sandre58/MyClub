// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.List;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyNet.UI.ViewModels.List.Grouping;
using MyNet.UI.ViewModels.List.Paging;
using MyNet.UI.ViewModels.List.Sorting;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.SchedulePage
{
    internal sealed class MatchesPlanningListParametersProvider : ListParametersProvider, IDisposable
    {
        private readonly IDisplayViewModel _displayViewModel;
        private readonly MatchesPlanningFiltersViewModel _filters;
        private readonly CompositeDisposable _disposables = [];

        public MatchesPlanningListParametersProvider(IEnumerable<IMatchParent> parents,
                                                     IEnumerable<DateTime> dates,
                                                     IEnumerable<TeamViewModel> teams,
                                                     IEnumerable<StadiumViewModel> stadiums,
                                                     SchedulingParametersViewModel schedulingParameters)
        {
            _filters = new(teams, stadiums, dates, parents, schedulingParameters);
            var displayModeDay = new DisplayModeDay(2, 12.Hours(), 23.Hours());
            _displayViewModel = new DisplayViewModel().AddMode(displayModeDay).AddMode<DisplayModeByParent>(true).AddMode<DisplayModeByDate>().AddMode<DisplayModeList>();
            _disposables.AddRange(
                [ _displayViewModel.WhenPropertyChanged(x => x.Mode).Subscribe(x =>
                    {
                        switch (x.Value)
                        {
                            case DisplayModeByDate:
                                _filters.FilterBy(FilterMode.Date);
                                break;
                            case DisplayModeByParent:
                                _filters.FilterBy(FilterMode.Parent);
                                break;
                            case DisplayModeDay:
                                _filters.FilterBy(FilterMode.DateRange);
                                break;
                            default:
                                _filters.FilterBy(FilterMode.None);
                                break;
                        }
                    }),
                   displayModeDay.WhenAnyPropertyChanged().Subscribe(_ =>
                   {
                       var filter = _filters.DateRangeFilter.Item.CastIn<DateFilterViewModel>();
                       filter.From = displayModeDay.DisplayDate.BeginningOfDay();
                       filter.To = displayModeDay.DisplayDate.AddDays(displayModeDay.DisplayDaysCount - 1).EndOfDay();
                   })]);
        }

        public override IFiltersViewModel ProvideFilters() => _filters;

        public override IGroupingViewModel ProvideGrouping()
            => new ExtendedGroupingViewModel(
            [
                new GroupingPropertyViewModel(nameof(MyClubResources.Matchday), (nameof(MatchViewModel.Parent))),
                new GroupingPropertyViewModel(nameof(MyClubResources.Date), (nameof(MatchViewModel.DateOfDay)))
            ]);

        public override ISortingViewModel ProvideSorting()
            => new ExtendedSortingViewModel(new Dictionary<string, string>
            {
                { nameof(MyClubResources.Matchday), nameof(MatchViewModel.Parent) },
                { nameof(MyClubResources.Date), nameof(MatchViewModel.StartDate) },
            }, new[] { nameof(MatchViewModel.StartDate) });

        public override IDisplayViewModel ProvideDisplay() => _displayViewModel;

        public override IPagingViewModel ProvidePaging() => new PagingViewModel(15);

        public void Dispose() => _disposables.Dispose();
    }

    internal class DisplayModeByParent : DisplayMode
    {
        public DisplayModeByParent() : base(nameof(DisplayModeByParent)) { }
    }

    internal class DisplayModeByDate : DisplayMode
    {
        public DisplayModeByDate() : base(nameof(DisplayModeByDate)) { }
    }
}
