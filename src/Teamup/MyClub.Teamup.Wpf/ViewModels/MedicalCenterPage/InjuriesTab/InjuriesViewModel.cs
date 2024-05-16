// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using MyNet.UI.ViewModels.List;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.MedicalCenterPage.InjuriesTab
{
    internal class InjuriesViewModel : SelectionListViewModel<InjuryViewModel>
    {
        private readonly PlayerPresentationService _playerPresentationService;

        public override bool CanOpen => true;

        public override bool CanAdd => true;

        public InjuriesViewModel(
            InjuriesProvider injuriesProvider,
            PlayerPresentationService playerPresentationService)
            : base(source: injuriesProvider.Connect(),
                  parametersProvider: new InjuriesListParametersProvider()) => _playerPresentationService = playerPresentationService;

        protected override async Task<InjuryViewModel?> UpdateItemAsync(InjuryViewModel oldItem)
        {
            await _playerPresentationService.EditInjuryAsync(oldItem).ConfigureAwait(false);

            return null;
        }

        protected override void OpenCore(InjuryViewModel? item, int? selectedTab = null) => item?.Open();

        public override async Task RemoveRangeAsync(IEnumerable<InjuryViewModel> oldItems) => await _playerPresentationService.RemoveInjuriesAsync(oldItems).ConfigureAwait(false);
    }
}
