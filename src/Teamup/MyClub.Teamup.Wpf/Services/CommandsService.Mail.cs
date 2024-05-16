// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.SendedMailAggregate;
using MyClub.UserContext.Domain.UserAggregate;
using MyNet.UI.Services;
using MyNet.UI.Toasting;
using MyNet.UI.Toasting.Settings;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Mail;

namespace MyClub.Teamup.Wpf.Services
{
    internal class MailCommandsService
    {
        private readonly IUserRepository _userRepository;
        private readonly SendedMailService _sendedMailService;
        private readonly IMailService _mailService;

        public MailCommandsService(IUserRepository userRepository, SendedMailService sendedMailService, IMailService mailService) => (_userRepository, _sendedMailService, _mailService) = (userRepository, sendedMailService, mailService);

        public static void OpenMailClient(IEnumerable<string>? addresses, string title = "", string body = "")
        {
            if (addresses is not null && addresses.Any())
            {
                try
                {
                    _ = MailToHelper.SendMail(addresses, title, body);
                }
                catch (Exception e)
                {
                    LogManager.Error(e);
                    ToasterManager.ShowError(MyClubResources.OpenMailClientError, ToastClosingStrategy.CloseButton);
                }
            }
        }

        public async Task SendMailAsync(IEmail email, bool sendACopy, Action<SendedMail?>? beforeSendCallback = null)
        {
            ToasterManager.ShowInformation(MyClubResources.SendMailInProgress);

            var user = _userRepository.GetCurrent();
            if (sendACopy && !string.IsNullOrEmpty(user.Email))
                _ = email.Bcc(user.Email, user.PreferredName);

            await AppBusyManager.BackgroundAsync(() =>
            {
                var sendedMail = _sendedMailService.Save(CreateDto(email, sendACopy));
                beforeSendCallback?.Invoke(sendedMail);
                var response = _mailService.Send(email);

                if (sendedMail is not null)
                    _ = _sendedMailService.UpdateState(sendedMail.Id, response.ErrorMessages.Count != 0 ? SendingState.Failed : SendingState.Success);

                if (response.ErrorMessages.Count != 0)
                {
                    ToasterManager.ShowError(MyClubResources.SendMailError);
                    response.ErrorMessages.ForEach(y => ToasterManager.ShowError(y, ToastClosingStrategy.CloseButton));
                }
                else
                {
                    ToasterManager.ShowSuccess(MyClubResources.SendMailSuccess);
                }
            }).ConfigureAwait(false);
        }

        public async Task SaveAsDraftAsync(IEmail email, bool sendACopy)
        {
            await AppBusyManager.BackgroundAsync(() => _sendedMailService.Save(CreateDto(email, sendACopy, SendingState.Draft))).ConfigureAwait(false);
            ToasterManager.ShowInformation(MyClubResources.SaveMailAsDraftSuccess);
        }

        private static SendedMailDto CreateDto(IEmail email, bool sendACopy, SendingState state = SendingState.InProgress)
            => new()
            {
                Body = !string.IsNullOrEmpty(email.Data.Body) ? email.Data.Body : email.Data.PlaintextAlternativeBody,
                Date = DateTime.UtcNow,
                SendACopy = sendACopy,
                State = state,
                Subject = email.Data.Subject,
                ToAddresses = email.Data.To.Select(x => x.Address).ToList()
            };
    }
}
