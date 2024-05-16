// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Domain.Services;
using MyClub.Teamup.Domain.ClubAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.SeasonAggregate;
using MyNet.Humanizer;
using MyNet.Utilities.Geography;

namespace MyClub.Teamup.Domain.Factories
{
    public class ProjectFactory(IAuditService auditService) : IProjectFactory
    {
        private readonly IAuditService _auditService = auditService;

        public Task<Project> CreateAsync(CancellationToken cancellationToken = default)
        {
            var season = Season.Current;
            var category = Category.Adult;
            var projectName = GetDefaultName(season, category);

            var club = new Club(MyClubResources.DefaultClubName)
            {
                AwayColor = "#FA5858",
                HomeColor = "#0040FF",
                Country = Country.France,
            };
            club.AddTeam(category, club.ShortName);

            var project = new Project(projectName, club, category, season, "#0040FF")
            {
                Preferences = new(new TimeSpan(19, 0, 0), new TimeSpan(1, 30, 0)),
                MainTeam = club.Teams[0]
            };

            _auditService.New(project);

            return Task.FromResult(project);
        }

        public static string GetDefaultName(Season season, Category category)
        {
            var startYear = season.Period.Start.Year;
            var endYear = season.Period.End.Year;
            var suffix = startYear == endYear ? startYear.ToString() : $"{season.Period.Start.Year}/{season.Period.End.Year}";
            return $"{category.Humanize()} {suffix}";
        }
    }
}
