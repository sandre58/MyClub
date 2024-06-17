// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reactive.Subjects;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.Wpf.Schedulers;
using MyNet.Observable.Collections;
using MyClub.Teamup.Wpf.Filters;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.CompetitionsPage;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.CompetitionPage
{
    internal class CompetitionPageViewModel<TCompetition> : ItemPageViewModel<CompetitionViewModel> where TCompetition : CompetitionViewModel
    {
        public CompetitionPageViewModel(ProjectInfoProvider projectInfoProvider, CompetitionsProvider competitionsProvider)
            : base(projectInfoProvider, CreateCollection(competitionsProvider), typeof(CompetitionsPageViewModel))
        {
        }

        public Subject<TCompetition?> CompetitionChanged { get; } = new();

        public TCompetition? Competition => Item as TCompetition;

        public CompetitionPageStadiumsViewModel StadiumsViewModel { get; } = new();

        public CompetitionPageRulesViewModel RulesViewModel { get; } = new();

        protected override IEnumerable<IFilterViewModel> ProvideOtherItemsFilters(CompetitionViewModel? item)
            => [new TeamsFilterViewModel($"{nameof(TrainingSessionViewModel.Teams)}", string.Empty) { Values = item?.Teams }];

        protected override void NavigateToItem(CompetitionViewModel item) => NavigationCommandsService.NavigateToCompetitionPage(item);

        protected override void OnItemChanged()
        {
            base.OnItemChanged();

            RaisePropertyChanged(nameof(Competition));
            CompetitionChanged.OnNext(Competition);
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            StadiumsViewModel.Dispose();
            RulesViewModel.Dispose();
        }

        private static ExtendedCollection<CompetitionViewModel> CreateCollection(CompetitionsProvider competitionsProvider)
        {
            var collection = new ExtendedCollection<CompetitionViewModel>(competitionsProvider.Connect(), WpfScheduler.Current);
            collection.SortingProperties.AscendingRange([nameof(CompetitionViewModel.Type), nameof(CompetitionViewModel.StartDate), nameof(CompetitionViewModel.Name)]);

            return collection;
        }
    }
}
