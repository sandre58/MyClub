// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Teamup.Wpf.Configuration;
using MyNet.Utilities.Mail;
using MyNet.Utilities.Mail.MailKit;
using MyNet.Utilities.Mail.Mock;
using MyNet.Utilities.Mail.Smtp;

namespace MyClub.Teamup.Wpf.Services.Factories
{
    internal class MailServiceFactory(TeamupConfiguration configuration) : IMailServiceFactory
    {
        private readonly TeamupConfiguration _configuration = configuration;

        public IMailService Create(SmtpClientOptions options) => !_configuration.DisableMail ? new MailKitService(options) : new MockMailService();
    }
}
