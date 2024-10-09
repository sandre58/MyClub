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
using MyClub.Scorer.Domain.BracketComputing;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.Factories;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.PersonAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.RankingAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyClub.Scorer.Mocks.Factory.Database.Resources;
using MyNet.Humanizer;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using MyNet.Utilities.Exceptions;
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
            var project = await CreateAsync(DatabaseContext.Domain.CompetitionAggregate.Competition.League, (name, image, matchFormat, matchRules, schedulingParameters) =>
            {
                var competition = new LeagueProject(name, image);
                competition.Competition.SchedulingParameters = schedulingParameters;
                competition.Competition.MatchFormat = matchFormat;
                competition.Competition.MatchRules = matchRules;
                return competition;
            }, true, 8, 20, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            // Penaly points
            var teams = RandomGenerator.ListItems(project.Teams, RandomGenerator.Int(0, 3));
            teams.ForEach(x => project.Competition.AddPenalty(x, RandomGenerator.Int(1, 10)));

            // Labels
            project.Competition.Labels.Add(new AcceptableValueRange<int>(1, RandomGenerator.Int(1, 3)), new RankLabel(RandomGenerator.Color(), MyClubResources.Promotion, MyClubResources.PromotionAbbr));
            project.Competition.Labels.Add(new AcceptableValueRange<int>(project.Teams.Count - RandomGenerator.Int(2, 4), project.Teams.Count), new RankLabel(RandomGenerator.Color(), MyClubResources.Relegation, MyClubResources.RelegationAbbr));

            // Scheduler
            var matchdaysScheduler = project.Competition.SchedulingParameters.AsSoonAsPossible
                ? new AsSoonAsPossibleStageScheduler<Matchday>()
                {
                    StartDate = project.Competition.SchedulingParameters.Start(),
                    Rules = [.. project.Competition.SchedulingParameters.AsSoonAsPossibleRules],
                    ScheduleVenues = true,
                    AvailableStadiums = project.Stadiums
                }
                : (IScheduler<Matchday>)new DateRulesStageScheduler<Matchday>()
                {
                    Interval = project.Competition.SchedulingParameters.Interval,
                    DateRules = [.. project.Competition.SchedulingParameters.DateRules],
                    TimeRules = [.. project.Competition.SchedulingParameters.TimeRules],
                    DefaultTime = project.Competition.SchedulingParameters.StartTime,
                    StartDate = project.Competition.SchedulingParameters.StartDate,
                };
            var venueScheduler = project.Competition.SchedulingParameters.UseHomeVenue ? (IMatchesScheduler)new HomeTeamVenueMatchesScheduler() : project.Competition.SchedulingParameters.AsSoonAsPossible ? null : new VenueRulesMatchesScheduler(project.Stadiums)
            {
                Rules = [.. project.Competition.SchedulingParameters.VenueRules],
            };

            // Algorithm
            var numberOfMatchesBetweenTeams = RandomGenerator.Int(1, 2);
            var algorithms = new List<IMatchdaysAlgorithm>
            {
                new RoundRobinAlgorithm() { NumberOfMatchesBetweenTeams = numberOfMatchesBetweenTeams, InvertTeamsByStage = numberOfMatchesBetweenTeams.Range().Select(x => x.IsEven()).ToArray() }
            };
            if (project.Teams.Count.IsEven())
                algorithms.Add(new SwissSystemAlgorithm() { NumberOfMatchesByTeams = RandomGenerator.Int(project.Teams.Count / 3, project.Teams.Count - 1) });

            var matchdays = new MatchdaysBuilder(matchdaysScheduler, venueScheduler).Build(project.Competition, RandomGenerator.ListItem(algorithms));

            matchdays.ForEach(x =>
            {
                x.Matches.ForEach(match =>
                {
                    if (match.Date.IsInPast())
                        match.Randomize(!match.GetPeriod().Contains(DateTime.UtcNow));
                    match.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);
                });
                x.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

                project.Competition.AddMatchday(x);
            });

            return project;
        }

        public async Task<CupProject> CreateCupAsync(CancellationToken cancellationToken = default)
            => await CreateAsync(DatabaseContext.Domain.CompetitionAggregate.Competition.Cup, (name, image, matchFormat, matchRules, schedulingParameters) =>
            {
                var competition = new CupProject(name, image);
                competition.Competition.SchedulingParameters = schedulingParameters;
                competition.Competition.MatchFormat = matchFormat;
                competition.Competition.MatchRules = matchRules;
                return competition;
            }, false, 16, 32, cancellationToken).ConfigureAwait(false);

        public async Task<TournamentProject> CreateTournamentAsync(CancellationToken cancellationToken = default)
            => await CreateAsync(DatabaseContext.Domain.CompetitionAggregate.Competition.Cup, (name, image, matchFormat, matchRules, schedulingParameters) =>
            {
                var competition = new TournamentProject(name, image);
                competition.Competition.SchedulingParameters = schedulingParameters;
                competition.Competition.MatchFormat = matchFormat;
                competition.Competition.MatchRules = matchRules;
                return competition;
            }, false, 32, 128, cancellationToken).ConfigureAwait(false);

        public Task<T> CreateAsync<T>(string type, Func<string, byte[]?, MatchFormat, MatchRules, SchedulingParameters, T> createInstance, bool allowDraw, int minTeams, int maxTeams, CancellationToken cancellationToken = default)
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

            using (_progresser.Start(3, new ProgressMessage(string.Empty)))
            {
                var allDatabaseCompetitions = unitOfWork.CompetitionRepository.GetAll().Where(x => x.Type == type).ToList();

                if (allDatabaseCompetitions.Count == 0) throw new TranslatableException(DatabaseResources.NoCompetitionsFoundError);

                var selectedCompetition = RandomGenerator.ListItem(allDatabaseCompetitions);
                T? project;
                var asSoonAsPossible = RandomGenerator.Bool();

                using (_progresser.Start(new ProgressMessage(string.Empty)))
                    project = createInstance(selectedCompetition.Name, selectedCompetition.Logo, CreateMatchFormat(asSoonAsPossible, allowDraw), CreateMatchRules(), CreateScheduleParameters(asSoonAsPossible));

                if (project is null) throw new InvalidOperationException($"Impossible to create an instance of {typeof(T)}");

                _logger.Trace($"Creation of project {project.Name}");

                cancellationToken.ThrowIfCancellationRequested();

                // Preferences
                project.Preferences.TreatNoStadiumAsWarning = RandomGenerator.Bool();
                project.Preferences.PeriodForPreviousMatches = asSoonAsPossible ? RandomGenerator.Int(1, 2).Hours() : RandomGenerator.Int(5, 8).Days();
                project.Preferences.PeriodForNextMatches = asSoonAsPossible ? RandomGenerator.Int(1, 2).Hours() : RandomGenerator.Int(5, 8).Days();

                // Teams
                using (_progresser.Start(new ProgressMessage(string.Empty)))
                {
                    var allDatabaseClubs = unitOfWork.ClubRepository.GetAll().ToList();
                    var allDatabasePlayers = unitOfWork.PlayerRepository.GetAll().ToList();
                    var availableTeams = allDatabaseClubs.SelectMany(x => x.Teams).ToList();
                    var teams = GetTeams(availableTeams, selectedCompetition.IsNational, GetIdFromDescription(selectedCompetition.Description, "id"), selectedCompetition.Country, RandomGenerator.Int(minTeams, maxTeams)).ToList();

                    if (teams.Count == 0) throw new TranslatableException(DatabaseResources.NoTeamsFoundError);

                    teams.ForEach(x =>
                    {
                        var convertedTeam = Convert(x, project.Competition.SchedulingParameters.UseHomeVenue);

                        var players = GetPlayers(allDatabasePlayers, x, RandomGenerator.Int(10, 20));
                        players.ForEach(y =>
                        {
                            var player = Convert(y, convertedTeam);
                            player.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);
                            convertedTeam.AddPlayer(player);
                        });

                        if (allDatabasePlayers.Count > 2)
                        {
                            convertedTeam.AddManagers(RandomGenerator.Int(1, 2).Range().Select(y =>
                            {
                                var manager = new Manager(convertedTeam, RandomGenerator.ListItem(allDatabasePlayers.Select(x => x.FirstName).ToList()).OrEmpty(), RandomGenerator.ListItem(allDatabasePlayers.Select(x => x.LastName).ToList()).OrEmpty())
                                {
                                    Country = RandomGenerator.Country(),
                                    Gender = GenderType.Male,
                                    LicenseNumber = string.Join("", RandomGenerator.Digits(10)),
                                };
                                manager.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

                                return manager;
                            }).ToList());
                        }

                        convertedTeam.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);
                        project.AddTeam(convertedTeam);

                        if (convertedTeam.Stadium is not null)
                        {
                            convertedTeam.Stadium.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);
                            project.AddStadium(convertedTeam.Stadium);
                        }
                    });
                }

                // Stadiums
                using (_progresser.Start(new ProgressMessage(string.Empty)))
                {
                    if (!project.Competition.SchedulingParameters.UseHomeVenue)
                    {
                        var allDatabaseStadiums = unitOfWork.StadiumRepository.GetAll().ToList();
                        var stadiums = GetRandomItems(allDatabaseStadiums, project.Competition.SchedulingParameters.AsSoonAsPossible ? RandomGenerator.Int(2, project.Teams.Count / 2) : RandomGenerator.Int(project.Teams.Count / 2, project.Teams.Count)).Select(Convert).ToList();
                        stadiums.ForEach(x =>
                        {
                            x.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);
                            project.AddStadium(x);
                        });
                    }
                }

                project.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

                return Task.FromResult(project);
            }
        }

        private static MatchRules CreateMatchRules() => MatchRules.Default;

        private static MatchFormat CreateMatchFormat(bool asSoonAsPossible, bool allowDraw) => asSoonAsPossible
                ? new MatchFormat(new HalfFormat(RandomGenerator.Int(1, 2), RandomGenerator.Int(8, 30).Minutes(), 5.Minutes()), allowDraw ? null : new HalfFormat(RandomGenerator.Int(1, 2), RandomGenerator.Int(4, 10).Minutes(), 2.Minutes()), allowDraw ? null : RandomGenerator.Int(3, 5))
                : (allowDraw ? MatchFormat.Default : MatchFormat.NoDraw);

        private static SchedulingParameters CreateScheduleParameters(bool asSoonAsPossible)
        {
            var useHomeVenue = RandomGenerator.Bool();
            return new SchedulingParameters(asSoonAsPossible ? DateTime.UtcNow.AddDays(-RandomGenerator.Int(1, 2)).ToDate() : DateTime.UtcNow.AddMonths(-RandomGenerator.Int(2, 4)).ToDate(),
                                            asSoonAsPossible ? DateTime.UtcNow.AddDays(RandomGenerator.Int(3, 6)).ToDate() : DateTime.UtcNow.AddMonths(RandomGenerator.Int(4, 6)).ToDate(),
                                            RandomGenerator.Int(8, 22).Hours().ToTime(),
                                            asSoonAsPossible ? RandomGenerator.Int(2, 5).Minutes() : RandomGenerator.Int(1, 3).Days(),
                                            asSoonAsPossible ? RandomGenerator.Int(5, 30).Minutes() : RandomGenerator.Int(1, 6).Days(),
                                            useHomeVenue,
                                            asSoonAsPossible,
                                            1.Days(),
                                            true,
                                            asSoonAsPossible ? [new IncludeTimePeriodsRule([new TimePeriod(RandomGenerator.Int(9, 14).Hours().ToTime(), RandomGenerator.Int(16, 20).Hours().ToTime())])] : [],
                                            !asSoonAsPossible ? [new IncludeDaysOfWeekRule(RandomGenerator.ListItems(Enum.GetValues<DayOfWeek>(), 1))] : [],
                                            [],
                                            !useHomeVenue && !asSoonAsPossible ? [new FirstAvailableStadiumRule(UseRotationTime.YesOrOtherwiseNo)] : []);
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

        private static Team Convert(DatabaseContext.Domain.ClubAggregate.Team team, bool withStadium)
            => new(team.Name, team.ShortName)
            {
                AwayColor = team.AwayColor ?? RandomGenerator.Color(),
                Country = team.Club.Country?.DehumanizeTo<Country>(OnNoMatch.ReturnsDefault),
                HomeColor = team.HomeColor ?? RandomGenerator.Color(),
                Logo = team.Club.Logo,
                Stadium = withStadium && team.Club.Stadium is not null ? Convert(team.Club.Stadium) : null
            };
        private static Stadium Convert(DatabaseContext.Domain.StadiumAggregate.Stadium stadium) => new(stadium.Name, stadium.Ground.DehumanizeTo<Ground>(OnNoMatch.ReturnsDefault))
        {
            Address = new Address(stadium.Street, stadium.PostalCode, stadium.City, stadium.Country?.DehumanizeTo<Country>(OnNoMatch.ReturnsDefault), stadium.Latitude, stadium.Longitude)
        };

        private static Player Convert(DatabaseContext.Domain.PlayerAggregate.Player player, Team team)
            => new(team, player.FirstName.OrEmpty(), player.LastName.OrEmpty())
            {
                Gender = player.Gender.DehumanizeTo<GenderType>(OnNoMatch.ReturnsDefault),
                Country = player.Country?.DehumanizeTo<Country>(OnNoMatch.ReturnsDefault),
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
