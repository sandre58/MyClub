// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.Notifications;
using MyNet.UI.Toasting;
using MyNet.UI.Toasting.Settings;
using MyNet.Utilities.Messaging;
using MyClub.Teamup.Application.Messages;
using MyClub.CrossCutting.Localization;

namespace MyClub.Teamup.Wpf.Services.Handlers
{
    internal sealed class TeamsValidationHandler : NotificationHandlerBase
    {
        private readonly ProjectCommandsService _projectCommandsService;
        public static readonly string TeamsOrderValidationCategory = "TeamsOrderValidation";

        public TeamsValidationHandler(ProjectCommandsService projectCommandsService)
        {
            _projectCommandsService = projectCommandsService;
            Messenger.Default.Register<TeamsOrderValidationMessage>(this, OnTeamsOrderValidation);
        }

        private void OnTeamsOrderValidation(TeamsOrderValidationMessage obj)
        {
            if (!obj.IsValid)
            {
                var notification = new ActionNotification(MyClubResources.ConsolidateTeamsWarning, MyClubResources.InvalidOrder, NotificationSeverity.Warning, TeamsOrderValidationCategory, false, async x =>
                {
                    ToasterManager.Hide(x);
                    await _projectCommandsService.EditTeamsAsync().ConfigureAwait(false);
                });
                ToasterManager.Show(notification, ToasterManager.SettingsFromSeverity(NotificationSeverity.Warning, ToastClosingStrategy.AutoClose), true, notification.Action);

                Notify(notification);
            }
            else
                Unnotify(x => x.Category == TeamsOrderValidationCategory);
        }

        protected override void Cleanup() => Messenger.Default.Unregister(this);

    }
}
