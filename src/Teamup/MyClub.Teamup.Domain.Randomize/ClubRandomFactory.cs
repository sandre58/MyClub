// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.ClubAggregate;
using MyClub.Teamup.Domain.Factories;
using MyClub.Teamup.Domain.TeamAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Generator;
using MyNet.Utilities.Generator.Extensions;
using MyNet.Utilities.Geography;

namespace MyClub.Teamup.Domain.Randomize
{
    public static class ClubRandomFactory
    {
        public static Club Random(string? name = null, Category? category = null, string? baseTeamName = null, string? baseTeamShortName = null, int countTeams = 1)
        {
            var clubName = name ?? AddressGenerator.City();
            var club = new Club(clubName, null)
            {
                AwayColor = RandomGenerator.Color(),
                HomeColor = RandomGenerator.Color(),
                Stadium = StadiumRandomFactory.Create(),
                Country = RandomGenerator.ListItem(Enumeration.GetAll<Country>().ToList()),
            };

            AddTeams(club, baseTeamName, baseTeamShortName, category, countTeams);

            club.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

            return club;
        }

        public static void AddTeams(Club club, string? baseName = null, string? baseShortName = null, Category? category = null, int countTeams = 1)
        {
            for (var i = 0; i < countTeams; i++)
            {
                var teamCategory = category ?? RandomGenerator.ListItem(Enumeration.GetAll<Category>());
                club.AddTeam(CreateTeam(club, teamCategory, baseName ?? club.Name, baseShortName ?? club.Name.GetInitials(), club.Teams.Where(x => x.Category == teamCategory).Select(x => (x.Name, x.ShortName, x.Order)).ToList()));
            }
        }

        private static Team CreateTeam(Club club, Category category, string baseName, string baseShortName, IList<(string name, string shortName, int order)> existingTeams)
        {
            var name = TeamFactory.GetTeamName(baseName, existingTeams.Select(x => x.name));
            var shortName = TeamFactory.GetTeamName(baseShortName, existingTeams.Select(x => x.shortName));
            var team = new Team(club, category, name, shortName)
            {
                Order = existingTeams.MaxOrDefault(x => x.order) + 1
            };
            team.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

            return team;
        }
    }
}
