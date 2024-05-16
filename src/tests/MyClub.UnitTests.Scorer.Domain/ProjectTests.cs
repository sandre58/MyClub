// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.ProjectAggregate;
using Xunit;

namespace MyClub.Scorer.UnitTests.Domain
{
    public class ProjectTests
    {
        [Fact]
        public void CreateLeagueTest()
        {
            var project = new LeagueProject("Ligue 1", DateTime.Today, DateTime.Today.AddMonths(12));

            Assert.Equal("Ligue 1", project.Name);
            Assert.Equal(CompetitionType.League, project.Type);
            Assert.IsAssignableFrom<Championship>(project.Competition);
        }

        [Fact]
        public void CreateCupTest()
        {
            var project = new CupProject("Coupe 1", DateTime.Today, DateTime.Today.AddMonths(12));

            Assert.Equal("Coupe 1", project.Name);
            Assert.Equal(CompetitionType.Cup, project.Type);
            Assert.IsAssignableFrom<Knockout>(project.Competition);
        }
    }
}
