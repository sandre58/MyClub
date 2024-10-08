﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyClub.CrossCutting.Images;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.ClubAggregate;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.Randomize;
using MyClub.Teamup.Domain.SeasonAggregate;
using MyNet.Humanizer;
using MyNet.Utilities;
using MyNet.Utilities.Generator;
using MyNet.Utilities.Generator.Extensions;
using MyNet.Utilities.Helpers;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Progress;

namespace MyClub.Teamup.Mocks.Factory.Random
{
    public class ProjectRandomFactory(IProgresser progresser, ILogger logger) : IProjectFactory
    {
        private readonly IProgresser _progresser = progresser;
        private readonly ILogger _logger = logger;

        public Task<Project> CreateAsync(CancellationToken cancellationToken = default)
        {
            using (_progresser.Start(3, new ProgressMessage(string.Empty)))
            {
                Club club;
                Project project;
                var season = Season.Current;
                var category = RandomGenerator.ListItem(Enumeration.GetAll<Category>());

                using (_progresser.Start(new ProgressMessage(string.Empty)))
                {
                    var logo = RandomProvider.GetTeamLogo();
                    var name = AddressGenerator.City().ToTitle();
                    club = ClubRandomFactory.Random(name, category, countTeams: RandomGenerator.Int(1, 3));
                    club.Logo = logo;

                    project = new Project($"{club.Name} {season.GetShortName()}", club, category, season, club.HomeColor ?? RandomGenerator.Color(), club.Logo)
                    {
                        Preferences = new(new TimeSpan(RandomGenerator.Int(6, 21), 0, 0), new TimeSpan(1, 30, 0)),
                        MainTeam = club.Teams[0]
                    };
                }

                _logger.Trace($"Creation of project {project.Name}");

                cancellationToken.ThrowIfCancellationRequested();

                // Players
                using (_progresser.Start(new ProgressMessage(string.Empty)))
                    club.Teams.ForEach(x => PlayerRandomFactory.RandomSquadPlayers(RandomGenerator.ListItem(Enumeration.GetAll<Category>()), x).ForEach(y =>
                    {
                        var player = project.AddPlayer(y);
                        player.Player.Photo = RandomProvider.GetPhoto(player.Player.Gender);
                    }));

                cancellationToken.ThrowIfCancellationRequested();

                // Competitions
                using (_progresser.Start(new ProgressMessage(string.Empty)))
                {
                    var teams = EnumerableHelper.Range(1, 30 * club.Teams.Count, 1).Select(_ => AddressGenerator.City()).Distinct().Select(x =>
                    {
                        var club = ClubRandomFactory.Random(x, project.Category);
                        club.Logo = RandomProvider.GetTeamLogo();

                        return club.Teams[0];
                    }).ToList();

                    cancellationToken.ThrowIfCancellationRequested();

                    // Friendlies
                    var teamsForFriendly = RandomGenerator.ListItems(teams, club.Teams.Count * 5).Concat(club.Teams).ToList();
                    var friendly = CompetitionRandomFactory.CreateFriendly(season, project.Category, teamsForFriendly, season.Period.Start.AddMonths(4), season.Period.Start.AddMonths(8), RandomGenerator.Int(8, 16));
                    friendly.Logo = RandomProvider.GetCompetitionLogo();
                    var seasonCompetitition = friendly.Seasons[0];
                    seasonCompetitition.GetAllMatches().Where(x => !club.Teams.Any(y => x.Participate(y))).ToList().ForEach(x => seasonCompetitition.RemoveMatch(x));
                    project.AddCompetition(seasonCompetitition);

                    cancellationToken.ThrowIfCancellationRequested();

                    // Leagues
                    club.Teams.ForEach(x =>
                    {
                        var teamsForLeague = RandomGenerator.ListItems(teams.Except(project.Competitions.OfType<LeagueSeason>().SelectMany(x => x.Teams)).ToList(), RandomGenerator.Int(9, 19)).Concat(new[] { x }).ToList();
                        var league = CompetitionRandomFactory.CreateLeague(season, project.Category, teamsForLeague, null, season.Period.Start, season.Period.End);
                        league.Logo = RandomProvider.GetCompetitionLogo();
                        project.AddCompetition(league.Seasons[0]);
                    });

                    cancellationToken.ThrowIfCancellationRequested();

                    // Cups
                    club.Teams.ForEach(x => EnumerableHelper.Iteration(RandomGenerator.Int(1, 3), _ =>
                    {
                        var teamsForCup = RandomGenerator.ListItems(teams, RandomGenerator.Int(5, 15)).Concat(new[] { x }).ToList();
                        var cup = CompetitionRandomFactory.CreateCup(season, project.Category, teamsForCup, null, season.Period.Start, season.Period.End);
                        cup.Logo = RandomProvider.GetCompetitionLogo();
                        project.AddCompetition(cup.Seasons[0]);
                    }));
                }

                project.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

                return Task.FromResult(project);
            }
        }
    }
}
