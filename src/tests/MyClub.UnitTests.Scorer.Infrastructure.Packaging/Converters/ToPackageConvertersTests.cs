using System;
using System.Collections.Generic;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Infrastructure.Packaging.Converters;
using MyClub.Scorer.Infrastructure.Packaging.Models;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using Xunit;

namespace MyClub.UnitTests.Scorer.Infrastructure.Packaging.Converters
{
    public class ToPackageConvertersTests
    {
        [Fact]
        public void ToPackage_IProject_LeagueType_CreatesCorrectPackage()
        {
            // Arrange
            var project = new LeagueProject("league 1");
            project.Competition.SchedulingParameters = new SchedulingParameters(DateTime.Today.BeginningOfYear(),
                                                                                DateTime.Today.EndOfYear(),
                                                                                15.Hours(),
                                                                                1.Days(),
                                                                                2.Days(),
                                                                                false,
                                                                                false,
                                                                                2.Days(),
                                                                                true,
                                                                                new List<IAvailableDateSchedulingRule>()
                                                                                {
                                                                                    new IncludeTimePeriodsRule([new TimePeriod(10.Hours(), 18.Hours(), DateTimeKind.Local)]),
                                                                                    new IncludeDaysOfWeekRule([DayOfWeek.Saturday, DayOfWeek.Sunday]),
                                                                                    new ExcludeDatesRangeRule(new DateTime(2024, 08, 19), new DateTime(2024, 08, 31))
                                                                                },
                                                                                [],
                                                                                [],
                                                                                []);

            // Act
            var result = project.ToPackage();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(CompetitionType.League, (CompetitionType)result.Metadata!.Type);
            Assert.Equal("league 1", result.Metadata.Name);
            Assert.Null(result.Metadata.Image);

            var leaguePackage = Assert.IsType<LeaguePackage>(result.Competition);
            Assert.NotNull(leaguePackage.SchedulingParameters);
        }
    }
}
