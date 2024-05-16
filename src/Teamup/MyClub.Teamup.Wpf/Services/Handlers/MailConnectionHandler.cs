// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.Dialogs;
using MyNet.UI.Notifications;
using MyNet.UI.Toasting;
using MyNet.UI.Toasting.Settings;
using MyNet.Utilities.Messaging;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Wpf.Messages;
using MyClub.Teamup.Wpf.ViewModels.Edition;

namespace MyClub.Teamup.Wpf.Services.Handlers
{
    internal sealed class MailConnectionHandler : NotificationHandlerBase
    {
        public static readonly string MailConnectionFailedCategory = "MailConnectionFailed";

        public MailConnectionHandler() => Messenger.Default.Register<MailConnectionCheckedMessage>(this, OnMailConnectionChecked);

        private void OnMailConnectionChecked(MailConnectionCheckedMessage obj)
        {
            if (!obj.IsSuccess)
            {
                var notification = new ActionNotification(MyClubResources.InvalidSmtpWarning, MyClubResources.SmtpErrorTitle, NotificationSeverity.Warning, MailConnectionFailedCategory, false, async x =>
                {
                    ToasterManager.Hide(x);
                    _ = await DialogManager.ShowDialogAsync<SettingsEditionViewModel>().ConfigureAwait(false);
                });
                ToasterManager.Show(notification, ToasterManager.SettingsFromSeverity(NotificationSeverity.Warning, ToastClosingStrategy.AutoClose), true, notification.Action);

                Notify(notification);
            }
            else
                Unnotify(x => x.Category == MailConnectionFailedCategory);
        }

        protected override void Cleanup() => Messenger.Default.Unregister(this);

    }
}
