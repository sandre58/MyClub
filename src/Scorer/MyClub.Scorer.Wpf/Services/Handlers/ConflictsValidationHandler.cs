// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.UI.Notifications;
using MyNet.UI.Toasting;
using MyNet.Utilities;
using MyNet.Utilities.Messaging;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Application.Messages;
using MyClub.Scorer.Domain.Enums;

namespace MyClub.Scorer.Wpf.Services.Handlers
{
    internal sealed class ConflictsValidationHandler : ProjectNotificationHandler
    {
        private const string Category = "Conflicts";

        private readonly MatchesProvider _matchesProvider;
        private List<(ConflictType, MatchViewModel, MatchViewModel?)> _currentConflicts = [];

        public ConflictsValidationHandler(MatchesProvider matchesProvider) : base(x => x.Category == Category)
        {
            _matchesProvider = matchesProvider;
            Messenger.Default.Register<MatchConflictsValidationMessage>(this, OnConflictsValidation);
        }

        private void OnConflictsValidation(MatchConflictsValidationMessage obj)
        {
            var newConflicts = obj.Conflicts.Select(x => (x.Item1, _matchesProvider.GetOrThrow(x.Item2), x.Item3.HasValue ? _matchesProvider.Get(x.Item3.Value) : null)).ToList();
            var hasNewConflicts = newConflicts.Exists(x => !_currentConflicts.Contains(x));

            Unnotify(x => x.Category == Category);

            if (newConflicts.Count > 0)
            {
                foreach (var conflict in newConflicts)
                {
                    var matchDisplayName = new Func<MatchViewModel, string>(x => $"{x.HomeTeam.Name} - {x.AwayTeam.Name} ({x.Parent.Name})");
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
