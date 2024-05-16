// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyNet.UI.Notifications;
using MyNet.Utilities.Messaging;
using MyClub.Scorer.Wpf.Messages;

namespace MyClub.Scorer.Wpf.Services.Handlers
{
    internal abstract class ProjectNotificationHandler : NotificationHandlerBase
    {
        private readonly Func<IClosableNotification, bool> _canRemoveWhenProjectChanging;

        protected ProjectNotificationHandler(Func<IClosableNotification, bool> canRemoveWhenProjectChanging)
        {
            _canRemoveWhenProjectChanging = canRemoveWhenProjectChanging;
            Messenger.Default.Register<CurrentProjectCloseRequestMessage>(this, OnCurrentProjectCloseRequest);
        }

        private void OnCurrentProjectCloseRequest(CurrentProjectCloseRequestMessage obj) => Unnotify(_canRemoveWhenProjectChanging);

        protected override void Cleanup() => Messenger.Default.Unregister(this);

    }
}
