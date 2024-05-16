// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Reactive.Subjects;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.List;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.CompetitionPage
{
    internal class FriendlyMatchesListParametersProvider(Subject<FriendlyViewModel?> friendlyChanged) : ListParametersProvider(nameof(MatchViewModel.Date))
    {
        private readonly Subject<FriendlyViewModel?> _friendlyChanged = friendlyChanged;

        public override IFiltersViewModel ProvideFilters() => new FriendlyMatchesSpeedFiltersViewModel(_friendlyChanged);

        public override IDisplayViewModel ProvideDisplay() => new DisplayViewModel().AddMode<DisplayModeGrid>(true).AddMode<DisplayModeList>();
    }
}
