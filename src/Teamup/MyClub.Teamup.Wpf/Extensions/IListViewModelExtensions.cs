// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.List.Filtering;
using MyClub.Teamup.Wpf.Filters;

namespace MyClub.Teamup.Wpf.Extensions
{
    internal static class IListViewModelExtensions
    {
        public static void ResetFiltersWithTeams(this IListViewModel listViewModel, IEnumerable<Guid>? teamIds, string propertyName = "TeamId")
        {
            var defaultFilters = new[] { new TeamsFilterViewModel(propertyName) { Values = teamIds } };
            if (listViewModel.Filters is ExtendedFiltersViewModel extendedFiltersViewModel)
            {
                extendedFiltersViewModel.SetDefaultFilters(defaultFilters);
                MyNet.UI.Threading.Scheduler.UI.Schedule(extendedFiltersViewModel.Reset);
            }
            else if (listViewModel.Filters is FiltersViewModel filtersViewModel)
            {
                MyNet.UI.Threading.Scheduler.UI.Schedule(() => filtersViewModel.Set(defaultFilters));
            }
        }
    }
}
