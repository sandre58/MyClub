// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using MyClub.Teamup.Domain.TeamAggregate;

namespace MyClub.Teamup.Wpf.Messages
{
    internal class MainTeamChangedMessage
    {
        public ReadOnlyCollection<Team> MainTeams { get; }

        public MainTeamChangedMessage(ReadOnlyCollection<Team> mainTeams) => MainTeams = mainTeams;
    }
}
