// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyNet.UI.Commands;
using MyNet.UI.Dialogs;
using MyNet.UI.Navigation.Models;
using MyNet.UI.Toasting;
using MyNet.UI.Toasting.Settings;
using MyNet.Utilities.Mail;
using MyNet.Utilities.Mail.Models;
using MyNet.UI.Resources;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Wpf.Extensions;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;

namespace MyClub.Teamup.Wpf.ViewModels.CommunicationPage
{
    internal class CommunicationPageViewModel : PageViewModel
    {
        private readonly IEmailFactory _emailFactory;
        private readonly MailCommandsService _mailCommandsService;

        public PlayersViewModel PlayersViewModel { get; }

        public EmailViewModel EmailViewModel { get; }

        public HistoryViewModel HistoryViewModel { get; }

        public bool ShowHistory { get; set; }

        public ICommand SendMailCommand { get; private set; }

        public ICommand OpenMailClientCommand { get; private set; }

        public ICommand SaveAsDraftCommand { get; private set; }

        public ICommand CloseHistoryCommand { get; }

        public ICommand ToggleHistoryCommand { get; }

        public CommunicationPageViewModel(
            ProjectInfoProvider projectInfoProvider,
            PlayersProvider playersProvider,
            SendedMailsProvider sendedMailsProvider,
            SendedMailPresentationService sendedMailPresentationService,
            IEmailFactory emailFactory,
            MailCommandsService mailCommandsService) : base(projectInfoProvider)
        {
            _emailFactory = emailFactory;
            _mailCommandsService = mailCommandsService;

            PlayersViewModel = new PlayersViewModel(playersProvider);
            EmailViewModel = new EmailViewModel();
            HistoryViewModel = new HistoryViewModel(sendedMailsProvider, sendedMailPresentationService);

            SendMailCommand = CommandsManager.Create(async () => await SendMailAsync().ConfigureAwait(false), () => PlayersViewModel.SelectedEmails.Any());
            OpenMailClientCommand = CommandsManager.Create(OpenMailClient, () => PlayersViewModel.SelectedEmails.Any());
            SaveAsDraftCommand = CommandsManager.Create(async () => await SaveAsDraftAsync().ConfigureAwait(false));
            CloseHistoryCommand = CommandsManager.Create(() => ShowHistory = false);
            ToggleHistoryCommand = CommandsManager.Create(() => ShowHistory = !ShowHistory);
        }

        protected override string CreateTitle() => MyClubResources.Communication;

        protected override void ResetFromMainTeams(IEnumerable<Guid>? mainTeams)
        {
            base.ResetFromMainTeams(mainTeams);
            PlayersViewModel.ResetFiltersWithTeams(mainTeams);
        }

        public override async Task ResetAsync()
        {
            if (!IsModified()) return;

            if (await DialogManager.ShowQuestionAsync(MessageResources.ItemModificationCancellingQuestion).ConfigureAwait(false) == MessageBoxResult.Yes)
            {
                using (IsModifiedSuspender.Suspend())
                {
                    EmailViewModel.Reset();
                    PlayersViewModel.Reset();

                    ResetIsModified();
                }
            }
        }

        public override void LoadParameters(INavigationParameters? parameters)
        {
            if (parameters is null) return;

            if (parameters.Has(NavigationCommandsService.SubjectParameterKey))
                EmailViewModel.Subject = parameters.Get<string>(NavigationCommandsService.SubjectParameterKey) ?? string.Empty;

            if (parameters.Has(NavigationCommandsService.SendCopyParameterKey))
                EmailViewModel.SendACopy = parameters.Get<bool>(NavigationCommandsService.SendCopyParameterKey);

            if (parameters.Has(NavigationCommandsService.BodyParameterKey))
                EmailViewModel.Body = parameters.Get<string>(NavigationCommandsService.BodyParameterKey) ?? string.Empty;

            if (parameters.Has(NavigationCommandsService.AddressesParameterKey))
            {
                var emails = parameters.Get<IEnumerable<string>>(NavigationCommandsService.AddressesParameterKey);
                if (emails is not null) PlayersViewModel.SelectAddresses(emails);
            }
        }

        private async Task SendMailAsync()
        {
            if (ValidateProperties())
                await _mailCommandsService.SendMailAsync(CreateEmail(), EmailViewModel.SendACopy, x => Reset()).ConfigureAwait(false);
            else
            {
                GetErrors().ToList().ForEach(x => ToasterManager.ShowError(x, ToastClosingStrategy.AutoClose));
            }

            await Task.CompletedTask;
        }

        private async Task SaveAsDraftAsync()
        {
            if (ValidateProperties())
            {
                await _mailCommandsService.SaveAsDraftAsync(CreateEmail(), EmailViewModel.SendACopy).ConfigureAwait(false);
                Reset();
            }
            else
            {
                GetErrors().ToList().ForEach(x => ToasterManager.ShowError(x, ToastClosingStrategy.AutoClose));
            }

            await Task.CompletedTask;
        }

        private void OpenMailClient()
        {
            if (ValidateProperties())
                MailCommandsService.OpenMailClient(PlayersViewModel.SelectedEmails.Select(x => x.Value), EmailViewModel.Subject, EmailViewModel.Body);
            else
                GetErrors().ToList().ForEach(x => ToasterManager.ShowError(x, ToastClosingStrategy.AutoClose));
        }

        private IEmail CreateEmail()
        {
            var email = _emailFactory.Create()
                        .Subject(EmailViewModel.Subject)
                        .Body(EmailViewModel.Body, true)
                        .To(PlayersViewModel.SelectedEmails.Select(x => new EmailAddress(x.Value)).ToList());

            if (EmailViewModel.Attachments.Any())
                EmailViewModel.Attachments.ToList().ForEach(x => email.AttachFromFilename(x));

            return email;
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            EmailViewModel.Dispose();
            PlayersViewModel.Dispose();
        }
    }
}
