// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyClub.CrossCutting.Localization;
using MyClub.DatabaseContext.Infrastructure.Data;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.Factories;
using MyClub.Scorer.Domain.Factories.Extensions;
using MyClub.Scorer.Domain.PersonAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.RankingAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Humanizer;
using MyNet.Utilities;
using MyNet.Utilities.Generator;
using MyNet.Utilities.Geography;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Progress;
using MyNet.Utilities.Sequences;

namespace MyClub.Scorer.Mocks.Factory.Database
{
    public class ProjectDatabaseFactory(IProgresser progresser, ILogger logger) : IProjectFactory
    {
        private readonly IProgresser _progresser = progresser;
        private readonly ILogger _logger = logger;

        public async Task<LeagueProject> CreateLeagueAsync(CancellationToken cancellationToken = default)
        {
            var project = await CreateAsync(DatabaseContext.Domain.CompetitionAggregate.Competition.League, (x, y, z) => new LeagueProject(x, y, z), 8, 20, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            // Penaly points
            var teams = RandomGenerator.ListItems(project.Teams, RandomGenerator.Int(0, 3));
            teams.ForEach(x => project.Competition.AddPenalty(x, RandomGenerator.Int(1, 10)));

            // Labels
            project.Competition.Labels.Add(new AcceptableValueRange<int>(1, RandomGenerator.Int(1, 3)), new RankLabel(RandomGenerator.Color(), MyClubResources.Promotion, MyClubResources.PromotionAbbr));
            project.Competition.Labels.Add(new AcceptableValueRange<int>(project.Teams.Count - RandomGenerator.Int(2, 4), project.Teams.Count), new RankLabel(RandomGenerator.Color(), MyClubResources.Relegation, MyClubResources.RelegationAbbr));

            // Matches
            var scheduler = new MatchdaysByDayOfWeekBuilder()
            {
                StartDate = project.SchedulingParameters.StartDate,
                Time = project.SchedulingParameters.StartTime,
                UseTeamVenues = project.SchedulingParameters.UseTeamVenues,
                DayOfWeeks = [RandomGenerator.Enum<DayOfWeek>()]
            };
            var matchdays = scheduler.Build(project.Competition, RoundRobinAlgorithm.Default);
            matchdays.ForEach(x =>
            {
                var stadiums = new Stack<Stadium>(RandomGenerator.ListItems(project.Stadiums, x.Matches.Count));
                x.Matches.ForEach(match =>
                {
                    if (match.Stadium is null)
                    {
                        _ = stadiums.TryPop(out var stadium);
                        match.Stadium = stadium;
                        match.IsNeutralStadium = false;
                    }

                    if (match.Date.IsInPast())
                        match.RandomizeScore(!match.GetPeriod().Contains(DateTime.UtcNow));
                    match.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);
                });
                x.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

                project.Competition.AddMatchday(x);
            });

            return project;
        }

        public async Task<CupProject> CreateCupAsync(CancellationToken cancellationToken = default)
            => await CreateAsync(DatabaseContext.Domain.CompetitionAggregate.Competition.Cup, (x, y, z) => new CupProject(x, y, z), 16, 32, cancellationToken).ConfigureAwait(false);

        public async Task<TournamentProject> CreateTournamentAsync(CancellationToken cancellationToken = default)
            => await CreateAsync(DatabaseContext.Domain.CompetitionAggregate.Competition.Cup, (x, y, z) => new TournamentProject(x, y, z), 32, 128, cancellationToken).ConfigureAwait(false);

        public Task<T> CreateAsync<T>(string type, Func<string, byte[]?, SchedulingParameters, T> createInstance, int minTeams, int maxTeams, CancellationToken cancellationToken = default)
            where T : IProject
        {
            // Configuration
            var configuration = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.FullName)
                                .AddJsonFile("config/appsettings.json", optional: false, reloadOnChange: true)
                                .Build();

            var connectionString = configuration.GetConnectionString("Default");
            var optionsBuilder = new DbContextOptionsBuilder<MyClubContext>();
            optionsBuilder.UseSqlServer(connectionString);
            using var unitOfWork = new UnitOfWork(new MyClubContext(optionsBuilder.Options));

            using (_progresser.Start(2, new ProgressMessage(string.Empty)))
            {
                var allDatabaseCompetitions = unitOfWork.CompetitionRepository.GetAll().Where(x => x.Type == type).ToList();
                var selectedCompetition = RandomGenerator.ListItem(allDatabaseCompetitions);
                T? project;

                using (_progresser.Start(new ProgressMessage(string.Empty)))
                {
                    project = createInstance(selectedCompetition.Name, selectedCompetition.Logo, new SchedulingParameters(
                                                                                                                DateTime.Today.AddMonths(-RandomGenerator.Int(4, 6)),
                                                                                                                DateTime.Today.AddMonths(RandomGenerator.Int(4, 6)),
                                                                                                                RandomGenerator.Int(8, 22).Hours(),
                                                                                                                RandomGenerator.Int(1, 5).Days(),
                                                                                                                RandomGenerator.Int(1, 5).Days(),
                                                                                                                RandomGenerator.Bool()));
                }

                if (project is null) throw new InvalidOperationException($"Impossible to create an instance of {typeof(T)}");

                _logger.Trace($"Creation of project {project.Name}");

                cancellationToken.ThrowIfCancellationRequested();

                // Teams
                using (_progresser.Start(new ProgressMessage(string.Empty)))
                {
                    var allDatabaseClubs = unitOfWork.ClubRepository.GetAll().ToList();
                    var allDatabasePlayers = unitOfWork.PlayerRepository.GetAll().ToList();
                    var availableTeams = allDatabaseClubs.SelectMany(x => x.Teams).ToList();
                    var teams = GetTeams(availableTeams, selectedCompetition.IsNational, GetIdFromDescription(selectedCompetition.Description, "id"), selectedCompetition.Country, RandomGenerator.Int(minTeams, maxTeams)).ToList();
                    teams.ForEach(x =>
                    {
                        var convertedTeam = Convert(x);

                        var players = GetPlayers(allDatabasePlayers, x, RandomGenerator.Int(10, 20));
                        players.ForEach(y =>
                        {
                            var player = Convert(y, convertedTeam);
                            player.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);
                            convertedTeam.AddPlayer(player);
                        });

                        convertedTeam.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);
                        project.AddTeam(convertedTeam);

                        if (convertedTeam.Stadium is not null)
                        {
                            convertedTeam.Stadium.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);
                            project.AddStadium(convertedTeam.Stadium);
                        }
                    });
                }

                project.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

                return Task.FromResult(project);
            }
        }

        private static List<DatabaseContext.Domain.ClubAggregate.Team> GetTeams(IEnumerable<DatabaseContext.Domain.ClubAggregate.Team> allTeams,
                                                                                bool isNational,
                                                                                int? leagueId,
                                                                                string? country,
                                                                                int count)
        {
            var teams = new List<DatabaseContext.Domain.ClubAggregate.Team>();

            if (isNational)
                teams.AddRange(GetRandomItems(allTeams.Where(x => x.Club.IsNational).ToList(), count));
            else if (leagueId.HasValue)
                teams.AddRange(GetRandomItems(allTeams.Where(x => GetIdFromDescription(x.Club.Description, "league") == leagueId).ToList(), count));

            if (teams.Count < count && country is not null)
            {
                var allowedTeams = allTeams.Except(teams).ToList();
                teams.AddRange(GetRandomItems(allowedTeams.Where(x => x.Club.Country == country && x.Club.IsNational == isNational).ToList(), count - teams.Count));
            }

            if (teams.Count < count)
            {
                var allowedTeams = allTeams.Where(x => x.Club.IsNational == isNational).Except(teams).ToList();
                teams.AddRange(GetRandomItems(allowedTeams, count - teams.Count));
            }

            return teams;
        }

        private static List<DatabaseContext.Domain.PlayerAggregate.Player> GetPlayers(IEnumerable<DatabaseContext.Domain.PlayerAggregate.Player> allPlayers, DatabaseContext.Domain.ClubAggregate.Team team, int count)
        {
            var players = new List<DatabaseContext.Domain.PlayerAggregate.Player>();

            players.AddRange(GetRandomItems(allPlayers.Where(x => GetIdFromDescription(x.Description, "team") == GetIdFromDescription(team.Club.Description, "id")).ToList(), count));

            if (players.Count < count)
            {
                if (team.Club.IsNational)
                {
                    var allowedPlayers = allPlayers.Except(players).Where(x => x.Country == team.Club.Country).ToList();
                    players.AddRange(GetRandomItems(allowedPlayers, count - players.Count));
                }

                if (players.Count < count)
                {
                    var allowedPlayers = allPlayers.Except(players).ToList();
                    players.AddRange(GetRandomItems(allowedPlayers, count - players.Count));
                }
            }

            return players;
        }

        private static Team Convert(DatabaseContext.Domain.ClubAggregate.Team team)
            => new(team.Name, team.ShortName)
            {
                AwayColor = team.AwayColor ?? RandomGenerator.Color(),
                Country = team.Club.Country?.DehumanizeToNullable<Country>(OnNoMatch.ReturnsDefault),
                HomeColor = team.HomeColor ?? RandomGenerator.Color(),
                Logo = team.Club.Logo,
                Stadium = team.Club.Stadium is not null
                  ? new Stadium(team.Club.Stadium.Name, team.Club.Stadium.Ground.DehumanizeTo<Ground>(OnNoMatch.ReturnsDefault))
                  {
                      Address = new Address(team.Club.Stadium.Street, team.Club.Stadium.PostalCode, team.Club.Stadium.City, team.Club.Stadium.Country?.DehumanizeToNullable<Country>(OnNoMatch.ReturnsDefault), team.Club.Stadium.Latitude, team.Club.Stadium.Longitude)
                  }
                  : null
            };

        private static Player Convert(DatabaseContext.Domain.PlayerAggregate.Player player, Team team)
            => new(team, player.FirstName.OrEmpty(), player.LastName.OrEmpty())
            {
                Gender = player.Gender.DehumanizeTo<GenderType>(OnNoMatch.ReturnsDefault),
                Country = player.Country?.DehumanizeToNullable<Country>(OnNoMatch.ReturnsDefault),
                Photo = player.Photo
            };

        private static int GetIdFromDescription(string? description, string parameter)
        {
            var descriptionParams = description?.Split(",");
            var param = descriptionParams?.Select(x => x.Split("=")).Where(x => x.Length > 1).ToDictionary(x => x[0], x => x[1]);

            return param is not null ? int.TryParse(param.GetOrDefault(parameter), out var id) ? id : 0 : 0;
        }

        private static List<T> GetRandomItems<T>(IList<T> items, int count) => RandomGenerator.ListItems(items, Math.Min(items.Count, count));
    }
}
