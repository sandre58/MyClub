// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.Notifications;
using MyNet.UI.Toasting;
using MyNet.UI.Toasting.Settings;
using MyNet.Utilities;
using MyNet.Utilities.Messaging;
using MyNet.UI.Resources;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Wpf.Messages;

namespace MyClub.Teamup.Wpf.Services.Handlers
{
    internal sealed class DatabaseConnectionHandler : NotificationHandlerBase
    {
        public static readonly string DatabaseConnectionFailedCategory = "DatabaseConnection";

        public DatabaseConnectionHandler() => Messenger.Default.Register<DatabaseConnectionCheckedMessage>(this, OnDatabaseConnectionChecked);

        private void OnDatabaseConnectionChecked(DatabaseConnectionCheckedMessage obj)
        {
            if (!obj.IsSuccess)
            {
                var notification = new ClosableNotification(MyClubResources.DatabaseConnectionWarning.FormatWith(obj.Host, obj.Name), UiResources.Error, NotificationSeverity.Warning, DatabaseConnectionFailedCategory, true);
                ToasterManager.Show(notification, ToasterManager.SettingsFromSeverity(NotificationSeverity.Warning, ToastClosingStrategy.AutoClose), true);

                Notify(notification);
            }
            else
                Unnotify(x => x.Category == DatabaseConnectionFailedCategory);
        }

        protected override void Cleanup() => Messenger.Default.Unregister(this);

    }
}
