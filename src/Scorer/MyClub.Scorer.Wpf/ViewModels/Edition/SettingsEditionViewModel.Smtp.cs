// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Wpf.Messages;
using MyClub.Scorer.Wpf.Settings;
using MyNet.Observable.Attributes;
using MyNet.UI.Commands;
using MyNet.UI.Toasting;
using MyNet.UI.ViewModels;
using MyNet.Utilities.Encryption;
using MyNet.Utilities.Mail;
using MyNet.Utilities.Mail.Smtp;
using MyNet.Utilities.Messaging;
using PropertyChanged;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class SmtpSettingsEditionViewModel : SettingsEditionTabViewModel
    {
        private readonly IEncryptionService _encryptionService;
        private readonly IMailServiceFactory _mailServiceFactory;

        public string? SmtpServer { get; set; }

        public int? SmtpPort { get; set; }

        public bool SmtpUseSsl { get; set; }

        public bool SmtpRequiresAuthentication { get; set; }

        public string? SmtpUsername { get; set; }

        public string? SmtpPassword { get; set; }

        public string? MailFromAddress { get; set; }

        public string? MailFromDisplayName { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool? ConnectionIsValid { get; private set; } = null;

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool ConnectionIsChecking { get; private set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool SmtpChanged { get; private set; }

        public ICommand TestConnectionCommand { get; set; }

        public SmtpSettingsEditionViewModel(
            IMailServiceFactory mailServiceFactory,
            IEncryptionService encryptionService)
        {
            Mode = ScreenMode.Edition;
            _mailServiceFactory = mailServiceFactory;
            _encryptionService = encryptionService;

            TestConnectionCommand = CommandsManager.Create(async () => await TestConnectionAsync().ConfigureAwait(false));
        }

        protected override string CreateTitle() => MyClubResources.SmtpSettings;

        protected override void RefreshCore()
        {
            base.RefreshCore();
            using (IsModifiedSuspender.Suspend())
            {
                SmtpChanged = false;
                ConnectionIsValid = null;
                ResetCore();
            }
        }

        protected override void ResetCore()
        {
            SmtpServer = SmtpSettings.Default.Server;
            SmtpPort = SmtpSettings.Default.Port;
            SmtpUseSsl = SmtpSettings.Default.UseSsl;
            SmtpRequiresAuthentication = SmtpSettings.Default.RequiresAuthentication;
            SmtpUsername = SmtpSettings.Default.Username;
            SmtpPassword = _encryptionService.Decrypt(SmtpSettings.Default.Password);
        }

        public override async void Save()
        {
            SmtpSettings.Default.Server = SmtpServer;
            SmtpSettings.Default.Port = SmtpPort ?? default;
            SmtpSettings.Default.UseSsl = SmtpUseSsl;
            SmtpSettings.Default.RequiresAuthentication = SmtpRequiresAuthentication;
            SmtpSettings.Default.Username = SmtpUsername;
            SmtpSettings.Default.Password = _encryptionService.Encrypt(SmtpPassword);

            SmtpSettings.Default.Save();

            if (ConnectionIsValid is not null)
                Messenger.Default.Send(new MailConnectionCheckedMessage(ConnectionIsValid.Value));
            else if (SmtpChanged)
            {
                await TestConnectionAsync().ConfigureAwait(false);
                Messenger.Default.Send(new MailConnectionCheckedMessage(ConnectionIsValid!.Value));
            }

            ConnectionIsValid = null;
            SmtpChanged = false;
        }

        private async Task TestConnectionAsync()
        {
            ConnectionIsChecking = true;
            var service = _mailServiceFactory.Create(new SmtpClientOptions
            {
                Server = SmtpServer,
                Port = SmtpPort ?? default,
                Password = SmtpPassword,
                RequiresAuthentication = SmtpRequiresAuthentication,
                User = SmtpUsername,
                UseSsl = SmtpUseSsl
            });

            ConnectionIsValid = await service.CanConnectAsync().ConfigureAwait(false);

            ConnectionIsChecking = false;

            if (!ConnectionIsValid.Value)
                ToasterManager.ShowError(MyClubResources.SmtpConnectionError);
            else
                ToasterManager.ShowSuccess(MyClubResources.SmtpConnectionSuccess);
        }

        [SuppressPropertyChangedWarnings]
        private void OnSmtpChanged()
        {
            if (!IsModifiedSuspender.IsSuspended)
            {
                SmtpChanged = true;
                ConnectionIsValid = null;
            }
        }

        public override string[] GetPropertiesAppliedAfterRestart() => [nameof(SmtpServer), nameof(SmtpPort), nameof(SmtpPassword), nameof(SmtpRequiresAuthentication), nameof(SmtpUsername), nameof(SmtpUseSsl)];

        protected virtual void OnSmtpServerChanged() => OnSmtpChanged();

        protected virtual void OnSmtpPortChanged() => OnSmtpChanged();

        protected virtual void OnSmtpPasswordChanged() => OnSmtpChanged();

        protected virtual void OnSmtpUsernameChanged() => OnSmtpChanged();

        protected virtual void OnSmtpRequiresAuthenticationChanged() => OnSmtpChanged();

        protected virtual void OnSmtpUseSslChanged() => OnSmtpChanged();
    }
}
