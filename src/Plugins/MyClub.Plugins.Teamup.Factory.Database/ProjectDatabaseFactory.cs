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
using MyClub.Teamup.Domain.ClubAggregate;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.Randomize;
using MyClub.Teamup.Domain.SeasonAggregate;
using MyClub.Teamup.Domain.StadiumAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyNet.Humanizer;
using MyNet.Utilities;
using MyNet.Utilities.Generator;
using MyNet.Utilities.Geography;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Progress;
using MyNet.Utilities.Sequences;

namespace MyClub.Plugins.Teamup.Factory.Database
{
    public class ProjectDatabaseFactory(IProgresser progresser, ILogger logger) : IProjectFactory
    {
        private readonly IProgresser _progresser = progresser;
        private readonly ILogger _logger = logger;

        public Task<Project> CreateAsync(CancellationToken cancellationToken = default)
        {
            // Configuration
            var configuration = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.FullName)
                                .AddJsonFile("config/appsettings.json", optional: false, reloadOnChange: true)
                                .Build();

            var connectionString = configuration.GetConnectionString("Default");
            var optionsBuilder = new DbContextOptionsBuilder<MyTeamup>();
            optionsBuilder.UseSqlServer(connectionString);
            using var unitOfWork = new UnitOfWork(new MyTeamup(optionsBuilder.Options));

            using (_progresser.Start(3, new ProgressMessage(string.Empty)))
            {
                var season = Season.Current;
                var allDatabaseClubs = unitOfWork.ClubRepository.GetAll().Where(x => x.Teams.Count > 0).ToList();
                var selectedClub = RandomGenerator.ListItem(allDatabaseClubs);
                var selectedTeam = selectedClub.Teams.First();
                var selectedCategory = selectedTeam.Category?.DehumanizeToNullable<Category>(OnNoMatch.ReturnsDefault) ?? RandomGenerator.ListItem(Enumeration.GetAll<Category>());
                var selectedStadium = selectedClub.Stadium ?? selectedTeam.Stadium;
                Club club;
                Project project;

                using (_progresser.Start(new ProgressMessage(string.Empty)))
                {
                    club = new Club(selectedClub.Name)
                    {
                        Logo = selectedClub.Logo,
                        AwayColor = selectedClub.HomeColor ?? RandomGenerator.Color(),
                        HomeColor = selectedClub.AwayColor ?? RandomGenerator.Color(),
                        Stadium = selectedStadium is not null
                                  ? new Stadium(selectedStadium.Name, selectedStadium.Ground.DehumanizeTo<Ground>(OnNoMatch.ReturnsDefault))
                                  {
                                      Address = new Address(selectedStadium.Street, selectedStadium.PostalCode, selectedStadium.City, selectedStadium.Country?.DehumanizeToNullable<Country>(), selectedStadium.Latitude, selectedStadium.Longitude),
                                  }
                                  : null,
                        Country = selectedClub.Country?.DehumanizeToNullable<Country>(OnNoMatch.ReturnsDefault)
                    };

                    // Manager teams
                    selectedClub.Teams.ForEach(x => club.AddTeam(Convert(x, club)));

                    if (club.Teams.Count < 2)
                        ClubRandomFactory.AddTeams(club, selectedTeam.Name, selectedTeam.ShortName, selectedCategory, RandomGenerator.Int(0, 2));

                    project = new Project($"{club.Name} {season.GetShortName()}", club, selectedCategory, season, club.HomeColor ?? RandomGenerator.Color(), club.Logo)
                    {
                        Preferences = new(new TimeSpan(RandomGenerator.Int(6, 21), 0, 0), new TimeSpan(1, 30, 0)),
                        MainTeam = club.Teams[0]
                    };
                }

                _logger.Trace($"Creation of project {project.Name}");

                cancellationToken.ThrowIfCancellationRequested();

                // Players
                using (_progresser.Start(new ProgressMessage(string.Empty)))
                {
                    if (club.Teams.Any())
                    {
                        var allPlayers = unitOfWork.PlayerRepository.GetAll().ToList();

                        // First squad
                        AddPlayers(project, GetPlayers(allPlayers, selectedTeam, RandomGenerator.Int(15, 20)), project.Club.Teams[0]);

                        // Othersquad
                        club.Teams.Skip(1).ForEach(x =>
                        {
                            var excludeNames = project.Players.Select(y => y.Player.LastName).ToList();
                            var availablePlayers = allPlayers.Where(x => !excludeNames.Contains(x.LastName.OrEmpty())).ToList();
                            AddPlayers(project, GetPlayers(availablePlayers, selectedTeam, RandomGenerator.Int(15, 20)), x);
                        });
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Competitions
                using (_progresser.Start(new ProgressMessage(string.Empty)))
                {
                    if (club.Teams.Any())
                    {
                        var allCompetitions = unitOfWork.CompetitionRepository.GetAll().ToList();
                        var availableCompetitions = allCompetitions.Where(x => x.Category == selectedCategory.ToString() && (selectedClub.IsNational && x.IsNational || !x.IsNational && x.Country == selectedClub.Country)).ToList();
                        var availableTeams = allDatabaseClubs.Except([selectedClub]).SelectMany(x => x.Teams).Where(x => x.Category == selectedCategory.ToString()).ToList();
                        AddCompetitions(project, availableCompetitions, availableTeams, selectedClub.IsNational, GetIdFromDescription(selectedClub.Description, "league"), selectedClub.Country, club.Teams[0]);

                        club.Teams.Skip(1).ForEach(x =>
                        {
                            var excludeClubNames = project.Competitions.Where(y => !y.Teams.Contains(x)).SelectMany(y => y.Teams).Select(x => x.Club.Name).Distinct().ToList();
                            var excludeCompetitionNames = project.Competitions.Where(y => !y.Teams.Contains(x)).Distinct().ToList();
                            var newAvailableTeams = availableTeams.Where(y => !excludeClubNames.Contains(y.Club.Name)).ToList();
                            var newAvailableCompetitions = availableCompetitions.Where(y => !excludeCompetitionNames.Contains(y.Name)).ToList();
                            AddCompetitions(project, newAvailableCompetitions, newAvailableTeams, selectedClub.IsNational, GetIdFromDescription(selectedClub.Description, "league"), selectedClub.Country, x);
                        });
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                project.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

                return Task.FromResult(project);
            }
        }

        private static void AddCompetitions(Project project,
                                            IEnumerable<DatabaseContext.Domain.CompetitionAggregate.Competition> availableCompetitions,
                                            IEnumerable<DatabaseContext.Domain.ClubAggregate.Team> availableTeams,
                                            bool isNational,
                                            int leagueId,
                                            string? country,
                                            Team team)
        {
            // Friendlies
            var teamsForFriendly = GetTeams(availableTeams, isNational, null, null, RandomGenerator.Int(3, 6)).Select(x => FindOrConvert(x, project)).Concat(new[] { team }).ToList();
            var friendly = CompetitionRandomFactory.CreateFriendly(project.Season, project.Category, teamsForFriendly, project.Season.Period.Start, project.Season.Period.Start.AddMonths(1), RandomGenerator.Int(8, 16));
            var season = friendly.Seasons[0];
            season.GetAllMatches().Where(x => !x.Participate(team)).ToList().ForEach(x => season.RemoveMatch(x));
            project.AddCompetition(season);

            // Leagues
            var teamsForLeague = GetTeams(availableTeams, isNational, leagueId, country, RandomGenerator.Int(9, 19)).Select(x => FindOrConvert(x, project)).Concat(new[] { team }).ToList();
            var league = GetCompetitions(availableCompetitions, DatabaseContext.Domain.CompetitionAggregate.Competition.League, 1).FirstOrDefault();

            if (league is not null)
                AddCompetition(project, league, teamsForLeague);

            // Cups
            var cups = GetCompetitions(availableCompetitions, DatabaseContext.Domain.CompetitionAggregate.Competition.Cup, RandomGenerator.Int(1, 3)).ToList();
            cups.ForEach(x =>
            {
                var teamsForCup = GetTeams(availableTeams, isNational, null, country, RandomGenerator.Int(12, 32)).Select(x => FindOrConvert(x, project)).Concat(new[] { team }).ToList();
                AddCompetition(project, x, teamsForCup);
            });
        }

        private static void AddCompetition(Project project, DatabaseContext.Domain.CompetitionAggregate.Competition databaseCompetiton, IEnumerable<Team> teams)
        {
            var matchFormat = new MatchFormat(new HalfFormat(databaseCompetiton.RegulationTimeNumber,
                                                             databaseCompetiton.RegulationTimeDuration),
                                                             databaseCompetiton.ExtraTimeNumber.HasValue && databaseCompetiton.ExtraTimeDuration.HasValue ? new HalfFormat(databaseCompetiton.ExtraTimeNumber.Value, databaseCompetiton.ExtraTimeDuration.Value) : null,
                                                             databaseCompetiton.NumberOfPenaltyShootouts);
            var rankingRules = new RankingRules(databaseCompetiton.PointsByGamesWon ?? 3,
                                                databaseCompetiton.PointsByGamesDrawn ?? 1,
                                                databaseCompetiton.PointsByGamesLost ?? 0,
                                                databaseCompetiton.SortingColumns?.Split(",").Select(x => x.DehumanizeTo<RankingSortingColumn>(OnNoMatch.ReturnsDefault)).ToList() ?? RankingRules.DefaultSortingColumns,
                                                databaseCompetiton.RankLabels?.Split(";").Select(x =>
                                                {
                                                    var label = x.Split(",");
                                                    var min = label.Length > 0 && int.TryParse(label[0], out var v1) ? v1 : 1;
                                                    var max = label.Length > 1 && int.TryParse(label[1], out var v2) ? v2 : 1;
                                                    var name = label.Length > 2 ? label[2] : string.Empty;
                                                    var shortName = label.Length > 3 ? label[3] : string.Empty;
                                                    var color = label.Length > 4 ? label[4] : string.Empty;
                                                    var description = label.Length > 5 ? label[5] : string.Empty;

                                                    return (min, max, new RankLabel(color, name, shortName, description));
                                                }).ToDictionary(x => new AcceptableValueRange<int>(x.min, x.max), x => x.Item3));

            ICompetition competition;
            switch (databaseCompetiton.Type)
            {
                case DatabaseContext.Domain.CompetitionAggregate.Competition.League:
                    var league = CompetitionRandomFactory.CreateLeague(project.Season, project.Category, teams, databaseCompetiton.Name.OrEmpty(), project.Season.Period.Start, project.Season.Period.End);
                    league.Rules = new LeagueRules(matchFormat, rankingRules, databaseCompetiton.MatchTime ?? RandomGenerator.Int(1, 23).Hours());
                    competition = league;
                    break;
                case DatabaseContext.Domain.CompetitionAggregate.Competition.Cup:
                    var cup = CompetitionRandomFactory.CreateCup(project.Season, project.Category, teams, databaseCompetiton.Name.OrEmpty(), project.Season.Period.Start, project.Season.Period.End);
                    cup.Rules = new CupRules(matchFormat, databaseCompetiton.MatchTime ?? RandomGenerator.Int(1, 23).Hours());
                    competition = cup;
                    break;
                default:
                    throw new InvalidOperationException("Competition type is unknown");
            }

            competition.Logo = databaseCompetiton.Logo;
            competition.ShortName = databaseCompetiton.ShortName.OrEmpty();

            competition.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);
            project.AddCompetition(competition.Seasons.First());
        }

        private static void AddPlayers(Project project, IEnumerable<DatabaseContext.Domain.PlayerAggregate.Player> players, Team team)
            => players.ForEach(x =>
            {
                var randomPlayer = PlayerRandomFactory.Random();

                randomPlayer.LastName = x.LastName.OrEmpty();
                randomPlayer.FirstName = x.FirstName.OrEmpty();
                randomPlayer.Birthdate = x.Birthdate;
                randomPlayer.Address = null;
                randomPlayer.Country = x.Country?.DehumanizeToNullable<Country>(OnNoMatch.ReturnsDefault);
                randomPlayer.Category = x.Category?.DehumanizeToNullable<Category>(OnNoMatch.ReturnsDefault);
                randomPlayer.Gender = x.Gender.DehumanizeTo<GenderType>(OnNoMatch.ReturnsDefault);
                randomPlayer.Category = x.Category?.DehumanizeToNullable<Category>(OnNoMatch.ReturnsDefault);
                randomPlayer.Height = x.Height;
                randomPlayer.Weight = x.Weight;
                randomPlayer.PlaceOfBirth = x.PlaceOfBirth;
                randomPlayer.Photo = x.Photo;
                randomPlayer.Laterality = x.Laterality.DehumanizeTo<Laterality>(OnNoMatch.ReturnsDefault);
                randomPlayer.LicenseNumber = x.LicenseNumber;

                if (x.Injuries is not null && x.Injuries.Count != 0)
                    x.Injuries.ForEach(y => randomPlayer.AddInjury(y.StartDate, y.Condition.OrEmpty(), y.Severity.OrEmpty().DehumanizeTo<InjurySeverity>(OnNoMatch.ReturnsDefault), y.EndDate, y.Type.OrEmpty().DehumanizeTo<InjuryType>(OnNoMatch.ReturnsDefault), y.Category.OrEmpty().DehumanizeTo<InjuryCategory>(OnNoMatch.ReturnsDefault), y.Description));

                if (x.Emails is not null && x.Emails.Count != 0)
                    x.Emails.ForEach(y => randomPlayer.AddEmail(y.Value.OrEmpty(), y.Label, y.Default));

                if (x.Phones is not null && x.Phones.Count != 0)
                    x.Phones.ForEach(y => randomPlayer.AddPhone(y.Value.OrEmpty(), y.Label, y.Default));

                if (x.Positions is not null && x.Positions.Count != 0)
                    x.Positions.ForEach(y =>
                    {
                        var position = y.Position.OrEmpty().DehumanizeTo<Position>(OnNoMatch.ReturnsDefault);
                        var rating = y.Rating.OrEmpty().DehumanizeTo<PositionRating>(OnNoMatch.ReturnsDefault);
                        var isNatural = y.IsNatural;

                        if (randomPlayer.Positions.FirstOrDefault(z => z.Position == position) is RatedPosition ratedPosition)
                        {
                            ratedPosition.Rating = rating;
                            ratedPosition.IsNatural = isNatural;
                        }
                        else
                            randomPlayer.AddPosition(position, rating, isNatural);
                    });

                var randomSquadPlayer = PlayerRandomFactory.RandomSquadPlayer(randomPlayer, RandomGenerator.ListItem(Enumeration.GetAll<Category>()), team);
                randomSquadPlayer.Size = x.Size;
                randomSquadPlayer.Category = x.Category?.DehumanizeToNullable<Category>(OnNoMatch.ReturnsDefault);

                project.AddPlayer(randomSquadPlayer);
            });

        private static int GetIdFromDescription(string? description, string parameter)
        {
            var descriptionParams = description?.Split(",");
            var param = descriptionParams?.Select(x => x.Split("=")).Where(x => x.Length > 1).ToDictionary(x => x[0], x => x[1]);

            return param is not null ? int.TryParse(param.GetOrDefault(parameter), out var id) ? id : 0 : 0;
        }

        private static List<DatabaseContext.Domain.CompetitionAggregate.Competition> GetCompetitions(IEnumerable<DatabaseContext.Domain.CompetitionAggregate.Competition> allCompetitions, string type, int count)
        {
            var competitions = new List<DatabaseContext.Domain.CompetitionAggregate.Competition>();

            competitions.AddRange(GetRandomItems(allCompetitions.Where(x => x.Type == type).ToList(), count));
            return competitions;
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

        private static List<DatabaseContext.Domain.PlayerAggregate.Player> GetPlayers(IEnumerable<DatabaseContext.Domain.PlayerAggregate.Player> allPlayers, DatabaseContext.Domain.ClubAggregate.Team selectedTeam, int count)
        {
            var players = new List<DatabaseContext.Domain.PlayerAggregate.Player>();

            players.AddRange(GetRandomItems(allPlayers.Where(x => GetIdFromDescription(x.Description, "team") == GetIdFromDescription(selectedTeam.Club.Description, "id")).ToList(), count));

            if (players.Count < count)
            {
                if (selectedTeam.Club.IsNational)
                {
                    var allowedPlayers = allPlayers.Except(players).Where(x => x.Country == selectedTeam.Club.Country).ToList();
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

        private static List<T> GetRandomItems<T>(IList<T> items, int count) => RandomGenerator.ListItems(items, Math.Min(items.Count, count));

        private static Team FindOrConvert(DatabaseContext.Domain.ClubAggregate.Team databaseTeam, Project project)
        {
            if (project.Competitions.SelectMany(x => x.Teams).FirstOrDefault(x => x.Name == databaseTeam.Name && x.Category.ToString() == databaseTeam.Category) is Team team)
                return team;

            var club = new Club(databaseTeam.Club.Name)
            {
                AwayColor = databaseTeam.Club.AwayColor ?? RandomGenerator.Color(),
                Country = databaseTeam.Club.Country?.DehumanizeToNullable<Country>(OnNoMatch.ReturnsDefault),
                HomeColor = databaseTeam.Club.HomeColor ?? RandomGenerator.Color(),
                Logo = databaseTeam.Club.Logo,
                Stadium = databaseTeam.Club.Stadium is not null
                  ? new Stadium(databaseTeam.Club.Stadium.Name, databaseTeam.Club.Stadium.Ground.DehumanizeTo<Ground>(OnNoMatch.ReturnsDefault))
                  {
                      Address = new Address(databaseTeam.Club.Stadium.Street, databaseTeam.Club.Stadium.PostalCode, databaseTeam.Club.Stadium.City, databaseTeam.Club.Stadium.Country?.DehumanizeToNullable<Country>(OnNoMatch.ReturnsDefault), databaseTeam.Club.Stadium.Latitude, databaseTeam.Club.Stadium.Longitude)
                  }
                  : null
            };

            var result = Convert(databaseTeam, club);

            return club.AddTeam(result);
        }

        private static Team Convert(DatabaseContext.Domain.ClubAggregate.Team team, Club club)
        {
            var entity = new Team(club, team.Category.OrEmpty().DehumanizeTo<Category>(OnNoMatch.ReturnsDefault), team.Name, team.ShortName);

            if (team.AwayColor != null)
                entity.AwayColor.Override(team.AwayColor);

            if (team.HomeColor != null)
                entity.HomeColor.Override(team.HomeColor);

            if (team.Stadium is not null && team.Stadium.Name != club.Stadium?.Name)
                entity.Stadium.Override(new Stadium(team.Stadium.Name, team.Stadium.Ground.DehumanizeTo<Ground>(OnNoMatch.ReturnsDefault))
                {
                    Address = new Address(team.Stadium.Street, team.Stadium.PostalCode, team.Stadium.City, team.Stadium.Country?.DehumanizeToNullable<Country>(OnNoMatch.ReturnsDefault), team.Stadium.Latitude, team.Stadium.Longitude)
                });

            return entity;
        }
    }
}
