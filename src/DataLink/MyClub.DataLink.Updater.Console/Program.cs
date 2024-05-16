// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyClub.DatabaseContext.Domain;
using MyClub.DatabaseContext.Domain.ClubAggregate;
using MyClub.DatabaseContext.Domain.CompetitionAggregate;
using MyClub.DatabaseContext.Domain.PlayerAggregate;
using MyClub.DatabaseContext.Domain.StadiumAggregate;
using MyClub.DatabaseContext.Infrastructure.Data;
using MyClub.DataLink.Updater.Console;
using MyNet.Http;
using MyNet.Utilities;
using MyNet.Utilities.Generator;
using Newtonsoft.Json.Linq;

// Configuration
var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("config/appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

// Web Api
var host = configuration.GetSection("WebApi:Host").Value.OrEmpty();
var key = configuration.GetSection("WebApi:Key").Value.OrEmpty();
using var webApiService = new WebApiService(headers: new Dictionary<string, string> {
                                                                        { "x-rapidapi-host", host },
                                                                        { "x-rapidapi-key", key },
                                                                      }, toException: x => new InvalidOperationException((x as JObject)?.First?.First?.ToString() ?? string.Empty));

// Database
var connectionString = configuration.GetConnectionString(nameof(MyClub));
var optionsBuilder = new DbContextOptionsBuilder<MyTeamup>();
optionsBuilder.UseSqlServer(connectionString);
using var unitOfWork = new UnitOfWork(new MyTeamup(optionsBuilder.Options));

Console.WriteLine($"From {host}");
Console.WriteLine($"To {connectionString}");
Console.WriteLine();

// Parameters
var mode = configuration.GetSection("Mode").Value.OrEmpty();
var parameters = configuration.GetChildren()
                                .First(x => x.Key == "Parameters")
                                    .GetChildren()
                                        .First(x => x.Key == mode)
                                            .GetChildren()
                                                .ToDictionary(x => x.Key.ToLower(), x => x.Value.OrEmpty());
Console.WriteLine(string.Join(" | ", parameters.Select(x => $"{x.Key}={x.Value}")));
Console.WriteLine();

// Competitions
if (mode == "Competitions" || mode == "CompetitionsAndTeamsAndPlayers")
    _ = await importCompetitions(webApiService, unitOfWork, parameters, mode == "CompetitionsAndTeamsAndPlayers", mode == "CompetitionsAndTeamsAndPlayers").ConfigureAwait(false);

// Teams / Stadiums
if (mode == "Teams" || mode == "TeamsAndPlayers")
    _ = await importClubs(webApiService, unitOfWork, parameters, mode == "TeamsAndPlayers").ConfigureAwait(false);

// Players
if (mode == "Players")
    _ = await importPlayers(webApiService, unitOfWork, parameters).ConfigureAwait(false);

// Save all changes
unitOfWork.Save();


// ------------------
// Helper functions
// ------------------
static void importInDatabase<T>(IGenericRepository<T> repository, List<T> newItems, Func<T, string> logMessage) where T : Entity
{
    var allItems = repository.GetAll().ToList();

    var addCount = 0;
    var updateCount = 0;
    var index = 1;
    foreach (var item in newItems)
    {
        if (allItems.Find(x => x.IsSimilar(item)) is T similar)
        {
            similar.SetFrom(item);
            repository.Update(similar);
            Console.WriteLine($"    -{index++:000} | Update | {logMessage(item)}");
            updateCount++;
        }
        else
        {
            repository.Add(item);
            Console.WriteLine($"    -{index++:000} | Add | {logMessage(item)}");
            addCount++;
        }
    }

    Console.WriteLine($"{addCount} items added, {updateCount} items updated");
}

static int getIdFromDescription(string? description, string parameter)
{
    var descriptionParams = description?.Split(",");
    var param = descriptionParams?.Select(x => x.Split("=")).Where(x => x.Length > 1).ToDictionary(x => x[0], x => x[1]);

    return param is not null ? int.TryParse(param.GetOrDefault(parameter), out var id) ? id : 0 : 0;
}

static async Task<IEnumerable<Competition>> importCompetitions(WebApiService webApiService, UnitOfWork unitOfWork, Dictionary<string, string> parameters, bool addTeams, bool addPlayers, string? prefix = null)
{
    var result = await new CompetitionsService(webApiService).GetAsync(parameters).ConfigureAwait(false);
    var items = result.ToList();
    importInDatabase(unitOfWork.CompetitionRepository, items, x => $"{(!string.IsNullOrEmpty(prefix) ? $"[{prefix}] " : string.Empty)}{x.Name} [{x.ShortName}] ({x.Type})");

    if (addTeams)
    {
        var season = parameters["season"];
        var shuffleItems = RandomGenerator.Shuffle(items).ToList();
        foreach (var item in shuffleItems)
        {
            var idLeague = getIdFromDescription(item.Description, "id");
            _ = await importClubs(webApiService, unitOfWork, new Dictionary<string, string> { { "league", idLeague.ToString() }, { "season", season } }, addPlayers, item.Name).ConfigureAwait(false);
        }
    }
    return items;
}

static async Task<IEnumerable<Club>> importClubs(WebApiService webApiService, UnitOfWork unitOfWork, Dictionary<string, string> parameters, bool addPlayers, string? prefix = null)
{
    var result = await new ClubsService(webApiService).GetAsync(parameters).ConfigureAwait(false);
    var items = result.ToList();
    var allStadiums = unitOfWork.StadiumRepository.GetAll().ToList();
    foreach (var club in items)
    {
        var stadium = searchStadium(allStadiums, club.Stadium);

        if (stadium is not null)
        {
            club.Stadium = stadium;
            club.StadiumId = stadium.Id;
        }

        foreach (var team in club.Teams)
        {
            stadium = searchStadium(allStadiums, team.Stadium);

            if (stadium is not null)
            {
                team.Stadium = stadium;
                team.StadiumId = stadium.Id;
            }
        }
    }

    importInDatabase(unitOfWork.ClubRepository, items, x => $"{(!string.IsNullOrEmpty(prefix) ? $"[{prefix}] " : string.Empty)}{x.Name} ({x.Stadium?.Name}, {x.Stadium?.City})");

    if (addPlayers)
    {
        var shuffleItems = RandomGenerator.Shuffle(items).ToList();
        foreach (var item in shuffleItems)
        {
            var team = getIdFromDescription(item.Description, "id");
            _ = await importPlayers(webApiService, unitOfWork, new Dictionary<string, string> { { "team", team.ToString() } }, item.Name).ConfigureAwait(false);
        }
    }

    return items;
}

static async Task<IEnumerable<Player>> importPlayers(WebApiService webApiService, UnitOfWork unitOfWork, Dictionary<string, string> parameters, string? prefix = null)
{
    var result = await new SquadService(webApiService).GetAsync(parameters).ConfigureAwait(false);
    var shuffleItems = RandomGenerator.Shuffle(result.ToList()).ToList();
    var playersToAdd = new List<Player>();
    var allPlayersInDatabase = unitOfWork.PlayerRepository.GetAll().ToList();

    foreach (var item in shuffleItems)
    {
        var id = getIdFromDescription(item.Description, "id");

        if (searchPlayer(allPlayersInDatabase, id) is null)
        {
            var players = await new PlayersService(webApiService).GetAsync(new Dictionary<string, string> { { "id", id.ToString() }, { "season", DateTime.Today.AddMonths(-8).Year.ToString() } }).ConfigureAwait(false);
            var player = players.FirstOrDefault();

            if (player is not null)
                playersToAdd.Add(player);
        }
    }

    importInDatabase(unitOfWork.PlayerRepository, playersToAdd, x => $"{(!string.IsNullOrEmpty(prefix) ? $"[{prefix}] " : string.Empty)}{x.LastName} {x.FirstName} ({x.Country})");

    return playersToAdd;
}

static Stadium? searchStadium(List<Stadium> allStadiums, Stadium? stadium)
    => stadium != null ? allStadiums.Find(x => x.Name.Equals(stadium.Name, StringComparison.OrdinalIgnoreCase)) : null;

static Player? searchPlayer(List<Player> allPlayers, int id)
    => allPlayers.Find(x => getIdFromDescription(x.Description, "id") == id);
