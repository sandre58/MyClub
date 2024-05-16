// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Linq;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.ClubAggregate;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.SeasonAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Helpers;
using Xunit;

namespace MyClub.UnitTests.Teamup.Domain.SquadAggregate
{
    public class SquadTests
    {
        private static void AddPlayers(Project project, int number)
            => EnumerableHelper.Iteration(number, x => project.AddPlayer(new Player($"FirstName{x}", $"LastName{x}")));

        private static void AddTeam(Project project, int number)
            => EnumerableHelper.Iteration(number, x => project.Club.AddTeam(project.Category, project.Club.ShortName.IncrementAlpha(project.Club.Teams.Select(x => x.Name))));

        private static Project CreateProject() => new("Project", new Club("Club"), Category.Adult, Season.CurrentYear, "#000000");

        [Fact]
        public void PlayersAddedWhenAddPlayers()
        {
            var project = CreateProject();
            AddPlayers(project, 20);

            Assert.Equal(20, project.Players.Count);
        }

        [Fact]
        public void PlayersRemovedWhenRemovePlayers()
        {
            var project = CreateProject();
            AddPlayers(project, 20);

            project.RemovePlayer(project.Players[0]);
            project.RemovePlayer(project.Players[0]);

            Assert.Equal(18, project.Players.Count);
        }

        [Fact]
        public void SquadsAddedWhenAddSquads()
        {
            var project = CreateProject();
            AddTeam(project, 3);

            Assert.Equal(3, project.Club.Teams.Count);
        }

        [Fact]
        public void PlayersRemovedWhenRemoveSquads()
        {
            var project = CreateProject();
            AddTeam(project, 3);
            AddPlayers(project, 21);

            var i = 0;
            foreach (var item in project.Players)
            {
                item.Team = project.Club.Teams.ToList()[i % 3];
                i++;
            }

            project.RemoveTeam(project.Club.Teams[0], true);

            Assert.Equal(14, project.Players.Count);
            Assert.Equal(2, project.Club.Teams.Count);
        }

        [Fact]
        public void SquadOfPlayersIsNullWhenRemoveSquads()
        {
            var project = CreateProject();
            AddTeam(project, 3);
            AddPlayers(project, 21);

            var i = 0;
            foreach (var item in project.Players)
            {
                item.Team = project.Club.Teams.ToList()[i % 3];
                i++;
            }

            project.RemoveTeam(project.Club.Teams[0], false);

            Assert.Equal(21, project.Players.Count);
            Assert.Equal(7, project.Players.Count(x => x.Team is null));
            Assert.Equal(2, project.Club.Teams.Count);
        }
    }
}
