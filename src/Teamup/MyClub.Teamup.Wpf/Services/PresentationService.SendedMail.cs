// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyNet.UI.Dialogs;
using MyNet.UI.Services;
using MyNet.Humanizer;
using MyNet.UI.Resources;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.Services
{
    internal class SendedMailPresentationService(SendedMailService sendedMailService)
    {
        private readonly SendedMailService _sendedMailService = sendedMailService;

        public async Task RemoveAsync(IEnumerable<SendedMailViewModel> oldItems)
        {
            var idsList = oldItems.Select(x => x.Id).ToList();
            if (idsList.Count == 0) return;

            var cancel = await DialogManager.ShowQuestionAsync(nameof(MessageResources.XItemsRemovingQuestion).TranslateAndFormatWithCount(idsList.Count)!, UiResources.Removing).ConfigureAwait(false) != MessageBoxResult.Yes;

            if (!cancel)
            {
                await AppBusyManager.WaitAsync(() => _sendedMailService.Remove(idsList));
            }
        }
    }
}
