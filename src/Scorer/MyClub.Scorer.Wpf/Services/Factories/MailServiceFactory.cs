// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Wpf.Configuration;
using MyNet.Utilities.Mail;
using MyNet.Utilities.Mail.MailKit;
using MyNet.Utilities.Mail.Mock;
using MyNet.Utilities.Mail.Smtp;

namespace MyClub.Scorer.Wpf.Services.Factories
{
    internal class MailServiceFactory(ScorerConfiguration configuration) : IMailServiceFactory
    {
        private readonly ScorerConfiguration _configuration = configuration;

        public IMailService Create(SmtpClientOptions options) => !_configuration.DisableMail ? new MailKitService(options) : new MockMailService();
    }
}
