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

        public MatchesPlanningListParametersProvider(IEnumerable<IStageViewModel> competitionStages,
                                                     IEnumerable<DateOnly> dates,
                                                     IEnumerable<IVirtualTeamViewModel> teams,
                                                     IEnumerable<StadiumViewModel> stadiums,
                                                     SchedulingParametersViewModel schedulingParameters)
        {
            _filters = new(teams, stadiums, dates, competitionStages, schedulingParameters);
            var displayModeDay = new DisplayModeDay(2, 12.Hours(), 23.Hours());
            _displayViewModel = new DisplayViewModel().AddMode(displayModeDay).AddMode<DisplayModeByStage>(true).AddMode<DisplayModeByDate>().AddMode<DisplayModeList>();
            _disposables.AddRange(
                [ _displayViewModel.WhenPropertyChanged(x => x.Mode).Subscribe(x =>
                    {
                        switch (x.Value)
                        {
                            case DisplayModeByDate:
                                _filters.FilterBy(FilterMode.Date);
                                break;
                            case DisplayModeByStage:
                                _filters.FilterBy(FilterMode.CompetitionStage);
                                break;
                            default:
                                _filters.FilterBy(FilterMode.None);
                                break;
                        }
                    })
                   ]);
        }

        public override IFiltersViewModel ProvideFilters() => _filters;

        public override IGroupingViewModel ProvideGrouping()
            => new ExtendedGroupingViewModel(
            [
                new GroupingPropertyViewModel(nameof(MyClubResources.Matchday), nameof(MatchViewModel.Stage)),
                new GroupingPropertyViewModel(nameof(MyClubResources.Date), nameof(MatchViewModel.DateOfDay))
            ]);

        public override ISortingViewModel ProvideSorting()
            => new ExtendedSortingViewModel(new Dictionary<string, string>
            {
                { nameof(MyClubResources.Matchday), nameof(MatchViewModel.Stage) },
                { nameof(MyClubResources.Date), nameof(MatchViewModel.Date) },
            }, new[] { nameof(MatchViewModel.Date) });

        public override IDisplayViewModel ProvideDisplay() => _displayViewModel;

        public override IPagingViewModel ProvidePaging() => new PagingViewModel(15);

        public void Dispose() => _disposables.Dispose();
    }

    internal class DisplayModeByStage : DisplayMode
    {
        public DisplayModeByStage() : base(nameof(DisplayModeByStage)) { }
    }

    internal class DisplayModeByDate : DisplayMode
    {
        public DisplayModeByDate() : base(nameof(DisplayModeByDate)) { }
    }
}
