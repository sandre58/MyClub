// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using MyNet.UI.ViewModels.List;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.CommunicationPage
{
    internal class HistoryViewModel : SelectionListViewModel<SendedMailViewModel>
    {
        private readonly SendedMailPresentationService _sendedMailPresentationService;

        public HistoryViewModel(SendedMailsProvider sendedMailsProvider, SendedMailPresentationService sendedMailPresentationService)
            : base(source: sendedMailsProvider.Connect())
        {
            CanAdd = false;
            CanEdit = false;
            _sendedMailPresentationService = sendedMailPresentationService;
        }

        public override async Task RemoveRangeAsync(IEnumerable<SendedMailViewModel> oldItems) => await _sendedMailPresentationService.RemoveAsync(oldItems).ConfigureAwait(false);

        protected override async void OpenCore(SendedMailViewModel item, int? selectedTab = null) => await NavigationCommandsService.NavigateToCommunicationPageAsync(null, item.Subject, item.Body, item.SendACopy).ConfigureAwait(false);
    }
}
