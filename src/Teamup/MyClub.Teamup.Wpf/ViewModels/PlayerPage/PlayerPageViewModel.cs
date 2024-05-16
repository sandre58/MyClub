// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.Teamup.Application.Deferrers;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyClub.Teamup.Wpf.ViewModels.RosterPage;
using MyNet.Observable.Attributes;
using MyNet.Observable.Collections;
using MyNet.Observable.Collections.Sorting;
using MyNet.UI.Navigation.Models;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyNet.Utilities;
using MyNet.Utilities.Comparaison;
using MyNet.Wpf.Schedulers;

namespace MyClub.Teamup.Wpf.ViewModels.PlayerPage
{
    [CanBeValidated(false)]
    [CanSetIsModified(false)]
    internal class PlayerPageViewModel : ItemPageViewModel<PlayerViewModel>
    {
        public PlayerPageOverviewViewModel OverviewViewModel { get; }

        public PlayerPagePositionsViewModel PositionsViewModel { get; }

        public PlayerPageInjuriesViewModel InjuriesViewModel { get; }

        public PlayerPageTrainingViewModel TrainingViewModel { get; }

        public PlayerPageAbsencesViewModel AbsencesViewModel { get; }

        public PlayerPageCommunicationViewModel CommunicationViewModel { get; }

        public PlayerPageViewModel(ProjectInfoProvider projectInfoProvider,
                                   SendedMailsProvider sendedMailsProvider,
                                   HolidaysProvider holidaysProvider,
                                   PlayersProvider playersProvider,
                                   TrainingStatisticsRefreshDeferrer trainingStatisticsRefreshDeferrer)
            : base(projectInfoProvider, CreateCollection(playersProvider), typeof(RosterPageViewModel))
        {
            OverviewViewModel = new PlayerPageOverviewViewModel();
            PositionsViewModel = new PlayerPagePositionsViewModel();
            InjuriesViewModel = new PlayerPageInjuriesViewModel();
            TrainingViewModel = new PlayerPageTrainingViewModel(holidaysProvider, trainingStatisticsRefreshDeferrer);
            AbsencesViewModel = new PlayerPageAbsencesViewModel();
            CommunicationViewModel = new PlayerPageCommunicationViewModel(sendedMailsProvider);

            AddSubWorkspaces(
            [
                OverviewViewModel,
                PositionsViewModel,
                InjuriesViewModel,
                TrainingViewModel,
                AbsencesViewModel,
                CommunicationViewModel,
            ]);
        }

        protected override void OnItemChanged()
        {
            base.OnItemChanged();

            if (Item is null) return;

            ItemSubscriptions?.AddRange(
            [
                Item.WhenAnyPropertyChanged(nameof(PlayerViewModel.TeamId)).Subscribe(_ => FilterOtherItems())
            ]);
        }

        protected override IEnumerable<IFilterViewModel> ProvideOtherItemsFilters(PlayerViewModel? item)
            => [new ComparableFilterViewModel<Guid>(nameof(PlayerViewModel.TeamId), ComplexComparableOperator.EqualsTo, item?.TeamId, null)];

        protected override void NavigateToItem(PlayerViewModel item) => NavigationCommandsService.NavigateToPlayerPage(item);

        protected override void ResetFromProject(Project? project)
        {
            base.ResetFromProject(project);

            AbsencesViewModel.StartDate = project?.Season.Period.Start ?? DateTime.Today.FirstDayOfYear();
            AbsencesViewModel.EndDate = project?.Season.Period.End ?? DateTime.Today.LastDayOfYear();
        }

        public override void LoadParameters(INavigationParameters? parameters)
        {
            if (parameters is null) return;

            if (parameters.Get<InjuryViewModel>(NavigationCommandsService.ItemParameterKey) is InjuryViewModel item)
            {
                Item = item.Player;
                InjuriesViewModel.SelectedInjury = item;
                GoToTab(InjuriesViewModel);
            }
            else
                base.LoadParameters(parameters);
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            OverviewViewModel.Dispose();
            PositionsViewModel.Dispose();
            InjuriesViewModel.Dispose();
            TrainingViewModel.Dispose();
            AbsencesViewModel.Dispose();
            CommunicationViewModel.Dispose();
        }

        private static ExtendedCollection<PlayerViewModel> CreateCollection(PlayersProvider playersProvider)
        {
            var collection = new ExtendedCollection<PlayerViewModel>(playersProvider.Connect(), WpfScheduler.Current);
            collection.SortingProperties.Add(new SortingProperty(nameof(PlayerViewModel.InverseName)));

            return collection;
        }
    }
}
