// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.Observable.Collections;
using MyNet.Observable.Attributes;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.CompetitionsPage;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.MatchPage
{
    [CanBeValidated(false)]
    [CanSetIsModified(false)]
    internal class MatchPageViewModel : ItemPageViewModel<MatchViewModel>
    {
        public MatchPageViewModel(ProjectInfoProvider projectInfoProvider)
            : base(projectInfoProvider, CreateCollection(), typeof(CompetitionsPageViewModel))
        {
        }

        protected override void NavigateToItem(MatchViewModel item) => NavigationCommandsService.NavigateToMatchPage(item);

        private static ExtendedCollection<MatchViewModel> CreateCollection() => [];
    }
}
