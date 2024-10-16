// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Wpf.Messages;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.UI.Notifications;
using MyNet.UI.Toasting;
using MyNet.Utilities;
using MyNet.Utilities.Messaging;

namespace MyClub.Scorer.Wpf.Services.Handlers
{
    internal sealed class ConflictsValidationHandler : ProjectNotificationHandler
    {
        private const string Category = "Conflicts";

        private List<(ConflictType, MatchViewModel, MatchViewModel?)> _currentConflicts = [];

        public ConflictsValidationHandler() : base(x => x.Category == Category) => Messenger.Default.Register<MatchConflictsValidationMessage>(this, OnConflictsValidation);

        private void OnConflictsValidation(MatchConflictsValidationMessage obj)
        {
            var newConflicts = obj.Conflicts.ToList();
            var hasNewConflicts = newConflicts.Exists(x => !_currentConflicts.Contains(x));

            Unnotify(x => x.Category == Category);

            if (newConflicts.Count > 0)
            {
                foreach (var conflict in newConflicts)
                {
                    var matchDisplayName = new Func<MatchViewModel, string>(x => $"{x.Home.Team.Name} - {x.Away.Team.Name} ({(x.Parent.Stage is not ICompetitionViewModel ? $"{x.Parent.Stage.Name} - {x.Parent.Name}" : x.Parent.Name)})");
                    var firstMessage = conflict.Item3 is not null
                        ? $"{MyClubResources.ConflictBetweenMatchXAndMatchY.FormatWith(matchDisplayName(conflict.Item2), matchDisplayName(conflict.Item3))}"
                        : $"{MyClubResources.ConflictWithMatchX.FormatWith(matchDisplayName(conflict.Item2))}";
                    var message = $"{firstMessage}\n{GetMessage(conflict.Item1)}";
                    var notification = new ClosableNotification(message, MyClubResources.Conflict, NotificationSeverity.Warning, Category, false);

                    Notify(notification);
                }

                if (hasNewConflicts)
                    ToasterManager.ShowWarning(MyClubResources.NewConflictsWarning, isUnique: true);
            }

            _currentConflicts = newConflicts;
        }

        private static string GetMessage(ConflictType type)
            => type switch
            {
                ConflictType.StadiumOccupancy => MyClubResources.ConflictsStadiumOccupancyWarning,
                ConflictType.TeamBusy => MyClubResources.ConflictsTeamBusyWarning,
                ConflictType.RestTimeNotRespected => MyClubResources.ConflictsRestTimeNotRespectedWarning,
                ConflictType.RotationTimeNotRespected => MyClubResources.ConflictsRotationTimeNotRespectedWarning,
                ConflictType.StartDatePassed => MyClubResources.ConflictsStartDatePassedWarning,
                ConflictType.EndDatePassed => MyClubResources.ConflictsEndDatePassedWarning,
                _ => string.Empty,
            };
    }
}
