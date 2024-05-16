// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using MyNet.UI.Commands;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.SendedMailAggregate;
using MyClub.Teamup.Wpf.Services;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class SendedMailViewModel : EntityViewModelBase<SendedMail>
    {
        public string Subject => Item.Subject;

        public string? Body => Item.Body;

        public bool SendACopy => Item.SendACopy;

        public DateTime Date => Item.Date.ToLocalTime();

        public SendingState State => Item.State;

        public ObservableCollection<string> ToAddresses => Item.ToAddresses;

        public ICommand OpenCommand { get; }

        public SendedMailViewModel(SendedMail item) : base(item) => OpenCommand = CommandsManager.Create(async () => await NavigationCommandsService.NavigateToCommunicationPageAsync(null, Subject, Body, SendACopy).ConfigureAwait(false));
    }
}
