// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.CommunicationPage
{
    internal class PlayersListParametersProvider : ListParametersProvider
    {
        public PlayersListParametersProvider() : base(nameof(PlayerViewModel.InverseName)) { }

        public override IFiltersViewModel ProvideFilters() => new PlayersSpeedFiltersViewModel();
    }
}
