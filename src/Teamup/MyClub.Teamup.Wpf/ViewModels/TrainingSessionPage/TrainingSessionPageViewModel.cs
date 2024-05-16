// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData.Binding;
using MyClub.Teamup.Wpf.Filters;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.TrainingPage;
using MyNet.Observable.Collections;
using MyNet.Observable.Collections.Sorting;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.Utilities.Comparaison;
using MyNet.Wpf.Schedulers;

namespace MyClub.Teamup.Wpf.ViewModels.TrainingSessionPage
{
    internal class TrainingSessionPageViewModel : ItemPageViewModel<TrainingSessionViewModel>
    {
        public TrainingSessionPageOverviewViewModel OverviewViewModel { get; }

        public TrainingSessionPageAttendancesViewModel AttendancesViewModel { get; }

        public TrainingSessionPageViewModel(ProjectInfoProvider projectInfoProvider,
                                            TrainingSessionsProvider trainingSessionsProvider,
                                            TrainingSessionPresentationService trainingSessionPresentationService)
            : base(projectInfoProvider, CreateCollection(trainingSessionsProvider), typeof(TrainingPageViewModel))
        {
            OverviewViewModel = new TrainingSessionPageOverviewViewModel(3);
            AttendancesViewModel = new TrainingSessionPageAttendancesViewModel(trainingSessionPresentationService, ItemChanged);
            AddSubWorkspaces([OverviewViewModel, AttendancesViewModel]);
        }

        protected override void OnItemChanged()
        {
            base.OnItemChanged();

            if (Item is null) return;

            ItemSubscriptions?.Add(Item.WhenPropertyChanged(x => x.IsCancelled).Subscribe(x => AttendancesViewModel.IsEnabled = !x.Value));
        }

        protected override void NavigateToItem(TrainingSessionViewModel item) => NavigationCommandsService.NavigateToTrainingSessionPage(item);

        protected override IEnumerable<IFilterViewModel> ProvideOtherItemsFilters(TrainingSessionViewModel? item)
            => [new TeamsFilterViewModel($"{nameof(TrainingSessionViewModel.Teams)}.{nameof(TeamViewModel.Id)}", nameof(TeamViewModel.Id)) { LogicalOperator = LogicalOperator.Or, Values = Item?.Teams.Select(x => x.Id).ToList() }];

        private static ExtendedCollection<TrainingSessionViewModel> CreateCollection(TrainingSessionsProvider trainingSessionsProvider)
        {
            var collection = new ExtendedCollection<TrainingSessionViewModel>(trainingSessionsProvider.Connect(), WpfScheduler.Current);
            collection.SortingProperties.Add(new SortingProperty(nameof(TrainingSessionViewModel.StartDate)));

            return collection;
        }
    }
}
