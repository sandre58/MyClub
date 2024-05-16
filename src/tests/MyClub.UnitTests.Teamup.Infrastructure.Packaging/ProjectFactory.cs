// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MyNet.Utilities;
using MyNet.Utilities.Generator;
using MyNet.Utilities.Helpers;
using MyNet.Humanizer;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.Randomize;
using MyClub.Teamup.Domain.Randomize.Extensions;
using MyClub.Teamup.Domain.SeasonAggregate;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyNet.Utilities.Generator.Extensions;

namespace MyClub.UnitTests.Teamup.Infrastructure.Packaging
{
    internal static class ProjectFactory
    {
        public static Project Create()
        {
            Project project;
            var season = Season.Current;

            var category = RandomGenerator.ListItem(Enumeration.GetAll<Category>());
            var club = ClubRandomFactory.Random(AddressGenerator.City().ToTitle(), category, countTeams: RandomGenerator.Int(1, 3));

            project = new Project($"{club.Name} - {category} {season}", club, category, season, club.HomeColor ?? RandomGenerator.Color(), club.Logo)
            {
                Preferences = new(new TimeSpan(RandomGenerator.Int(6, 21), 0, 0), new TimeSpan(1, 30, 0)),
            };

            // Players
            club.Teams.ForEach(x => PlayerRandomFactory.RandomSquadPlayers(RandomGenerator.ListItem(Enumeration.GetAll<Category>()), x).ForEach(y => project.AddPlayer(y)));

            // Competitions
            var teams = EnumerableHelper.Range(1, 30 * club.Teams.Count, 1).Select(_ => AddressGenerator.City()).Distinct().Select(x => ClubRandomFactory.Random(category: project.Category).Teams[0]).ToList();

            // Friendlies
            var teamsForFriendly = RandomGenerator.ListItems(teams, club.Teams.Count * 5).Concat(club.Teams).ToList();
            var friendly = CompetitionRandomFactory.CreateFriendly(season, project.Category, teamsForFriendly, season.Period.Start.AddMonths(4), season.Period.Start.AddMonths(8), RandomGenerator.Int(8, 12));
            var seasonCompetitition = friendly.Seasons[0];
            seasonCompetitition.GetAllMatches().Where(x => !club.Teams.Any(y => x.Participate(y))).ToList().ForEach(x => seasonCompetitition.RemoveMatch(x));
            project.AddCompetition(seasonCompetitition);

            // Leagues
            club.Teams.ForEach(x =>
            {
                var teamsForLeague = RandomGenerator.ListItems(teams.Except(project.Competitions.OfType<LeagueSeason>().SelectMany(x => x.Teams)).ToList(), RandomGenerator.Int(9, 19)).Concat(new[] { x }).ToList();
                var league = CompetitionRandomFactory.CreateLeague(season, project.Category, teamsForLeague, null, season.Period.Start, season.Period.End);
                project.AddCompetition(league.Seasons[0]);
            });

            // Cups
            club.Teams.ForEach(x => EnumerableHelper.Iteration(RandomGenerator.Int(1, 3), _ =>
            {
                var teamsForCup = RandomGenerator.ListItems(teams, RandomGenerator.Int(5, 15)).Concat(new[] { x }).ToList();
                var cup = CompetitionRandomFactory.CreateCup(season, project.Category, teamsForCup, null, season.Period.Start, season.Period.End);
                project.AddCompetition(cup.Seasons[0]);
            }));

            // Tactics
            TacticRandomFactory.RandomKnownTactics().ForEach(x => project.AddTactic(x));
            EnumerableHelper.Iteration(RandomGenerator.Int(1, 2), x => project.AddTactic(TacticRandomFactory.Random($"{MyClubResources.Tactic} {x + 1}")));

            // Sended Mails
            SendedMailRandomFactory.RandomSendedMails(project.Players.SelectMany(x => x.Player.Emails).Select(x => x.Value).ToList(), project.Season.Period.Start).ToList().ForEach(x => project.AddSendedMail(x));

            // Periods
            var (cycles, holidays) = PeriodRandomFactory.RandomPeriods(project.Season.Period.Start, project.Season.Period.End);
            cycles.ToList().ForEach(x => project.AddCycle(x));
            holidays.ToList().ForEach(x => project.AddHolidays(x));

            // Matches
            project.Competitions.SelectMany(x => x.GetAllMatches()).ToList().ForEach(x => x.RandomizeScore());

            // Trainings
            TrainingSessionRandomFactory.RandomTrainingSessions(project.Season.Period.Start, project.Season.Period.End, club.Teams, project.Players).ToList().ForEach(x => project.AddTrainingSession(x));

            project.MarkedAsCreated(DateTime.UtcNow, "System");

            return project;
        }
    }
}
