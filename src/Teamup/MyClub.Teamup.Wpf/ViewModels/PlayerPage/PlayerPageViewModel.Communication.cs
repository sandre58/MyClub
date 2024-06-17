// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyNet.UI.ViewModels.Workspace;
using MyNet.UI.Threading;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.PlayerPage
{
    internal class PlayerPageCommunicationViewModel : SubItemViewModel<PlayerViewModel>
    {
        private readonly ObservableCollectionExtended<SendedMailViewModel> _sendedMails = [];

        public ReadOnlyObservableCollection<SendedMailViewModel> SendedMails { get; }

        public PlayerPageCommunicationViewModel(SendedMailsProvider sendedMailsProvider)
        {
            SendedMails = new(_sendedMails);

            var dynamicFilter = ItemChanged.Select(x => new Func<SendedMailViewModel, bool>(y => x is not null && y.ToAddresses.Any(z => x.Emails.Select(a => a.Value).Contains(z))));
            Disposables.Add(sendedMailsProvider.Connect().Filter(dynamicFilter).ObserveOn(Scheduler.UI).Bind(_sendedMails).Subscribe());
        }
    }
}
