// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.ClubAggregate;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.Randomize;
using MyClub.Teamup.Domain.SeasonAggregate;
using MyNet.Http;
using MyNet.Humanizer;
using MyNet.Utilities;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Extensions;
using MyNet.Utilities.Generator;
using MyNet.Utilities.Geography;
using MyNet.Utilities.Helpers;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Progress;
using Newtonsoft.Json.Linq;

namespace MyClub.Plugins.Teamup.Factory.Web
{
    public sealed class ProjectWebFactory(IProgresser progresser, ILogger logger) : IProjectFactory, IDisposable
    {
        private const string RequestTeamUrl = "https://v3.football.api-sports.io/players/squads";
        private const string RequestPlayerUrl = "https://v3.football.api-sports.io/players";
        private readonly WebApiService _webApiService = new(headers: new Dictionary<string, string> {
                                                                        { "x-rapidapi-host", "v3.football.api-sports.io" },
                                                                        { "x-rapidapi-key", "c9e0f946f6aa0c057f686755e1f32be3" },
                                                                      }, toException: x => new InvalidOperationException((x as JObject)?.First?.First?.ToString() ?? string.Empty));
        private readonly IProgresser _progresser = progresser;
        private readonly ILogger _logger = logger;
        private static readonly double[] SubStepDefinitions = [0.49, 5, 0.01];

        public async Task<Project> CreateAsync(CancellationToken cancellationToken = default)
        {
            using (_progresser.Start(SubStepDefinitions, new ProgressMessage(string.Empty)))
            {
                var season = Season.Current;
                string? clubName = null;
                byte[]? logo = null;
                JToken result;

                using (_progresser.Start(new ProgressMessage(MyClubResources.ProgressGetWebData)))
                {
                    var idClub = RandomGenerator.Int(1, 350);

                    var response = await _webApiService.GetDataAsync<object>(RequestTeamUrl, cancellationToken, ("team", idClub.ToString())).ConfigureAwait(false);
                    result = (response as JObject)!.GetValue("response")?.First!;

                    cancellationToken.ThrowIfCancellationRequested();

                    if (result is null)
                        throw new TranslatableException(nameof(MyClubResources.GetDataFromWebError));

                    var resultMeta = result.First?.First;

                    clubName = resultMeta?["name"]?.ToString();
                    var logoUrl = resultMeta?["logo"]?.ToString();
                    if (!string.IsNullOrEmpty(logoUrl))
                    {
                        try
                        {
                            logo = _webApiService.GetStream(logoUrl)?.ReadImage();
                        }
                        catch (Exception)
                        {
                            _logger.Warning($"Logo from url '{logoUrl}' not found.");
                        }
                    }
                }

                var category = RandomGenerator.ListItem(Enumeration.GetAll<Category>());
                var club = new Club(clubName.OrEmpty())
                {
                    HomeColor = RandomGenerator.Color(),
                    AwayColor = RandomGenerator.Color(),
                    Logo = logo,
                    Stadium = StadiumRandomFactory.Create(),
                    Country = RandomGenerator.ListItem(Enumeration.GetAll<Country>().ToList())
                };
                ClubRandomFactory.AddTeams(club, category: category, countTeams: RandomGenerator.Int(1, 3));

                var project = new Project($"{club.Name} {season.GetShortName()}", club, category, season, club.HomeColor ?? RandomGenerator.Color(), club.Logo)
                {
                    Preferences = new(new TimeSpan(RandomGenerator.Int(6, 21), 0, 0), new TimeSpan(1, 30, 0)),
                    MainTeam = club.Teams[0]
                };

                cancellationToken.ThrowIfCancellationRequested();

                //Players
                using (var step = _progresser?.Start(new ProgressMessage(string.Empty)))
                    await FillPlayersAsync(project, result!, step, cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                // Competitions
                using (_progresser?.Start(new ProgressMessage(string.Empty)))
                {
                    // Empty for now
                }

                project.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

                return project;
            }
        }

        private async Task FillPlayersAsync(Project project, JToken result, IProgressStep<ProgressMessage>? step, CancellationToken cancellationToken = default)
        {
            var resultPlayers = result.Last?.First;

            if (resultPlayers is not null)
            {
                var countPlayers = resultPlayers.Count();
                var index = 0;
                foreach (var item in resultPlayers)
                {
                    var id = item?["id"]?.ToString()!;

                    var response = await _webApiService.GetDataAsync<object>(RequestPlayerUrl, cancellationToken, ("id", id), ("season", project.Season.Period.Start.Year.ToString())).ConfigureAwait(false);
                    var playerResult = (response as JObject)?.GetValue("response")?.First?.First?.First;

                    var player = PlayerRandomFactory.Random();
                    var squadPlayer = PlayerRandomFactory.RandomSquadPlayer(player, team: RandomGenerator.ListItem(project.Club.Teams));

                    _logger.Trace($"New Player: {item?["name"]}{(playerResult is not null ? " (Details found in Web API)" : string.Empty)}");
                    if (playerResult is not null)
                    {
                        // Name
                        player.FirstName = playerResult?["firstname"]?.ToString() ?? string.Empty;
                        player.LastName = playerResult?["lastname"]?.ToString() ?? string.Empty;

                        // Country
                        player.Country = Array.Find(Enumeration.GetAll<Country>(), x => x.Name == playerResult?["nationality"]?.ToString() || x.Humanize() == playerResult?["nationality"]?.ToString() || x.Alpha3 == playerResult?["nationality"]?.ToString() || x.Alpha2 == playerResult?["nationality"]?.ToString());

                        // Birth
                        var birth = playerResult?["birth"];
                        player.PlaceOfBirth = birth?["place"]?.ToString();
                        if (DateTime.TryParse(birth?["date"]?.ToString(), CultureInfo.InvariantCulture, out var birthdate))
                            player.Birthdate = birthdate;

                        // Body
                        if (int.TryParse(playerResult?["height"]?.ToString().Split(" ")[0], out var height))
                            player.Height = height;
                        if (int.TryParse(playerResult?["weight"]?.ToString().Split(" ")[0], out var weight))
                            player.Weight = weight;
                    }
                    else
                    {
                        // Name
                        var splitName = item?["name"]?.ToString().Split(" ");
                        player.FirstName = splitName?.Length == 1 ? " " : splitName?[0] ?? string.Empty;
                        player.LastName = splitName?.Length == 1 ? splitName?[0] ?? string.Empty : string.Join(" ", splitName?.TakeLast(splitName.Length - 1) ?? []);

                        // Birthdate
                        if (int.TryParse(item?["age"]?.ToString(), out var age))
                        {
                            var date = DateTime.UtcNow.AddYears(-age);
                            player.Birthdate = RandomGenerator.Date(date.FirstDayOfYear(), date.LastDayOfMonth());
                        }
                    }

                    // Number
                    if (int.TryParse(item?["number"]?.ToString(), out var number))
                        squadPlayer.Number = number;

                    // Image
                    var photoUrl = playerResult?["photo"]?.ToString() ?? item?["photo"]?.ToString();
                    if (!string.IsNullOrEmpty(photoUrl))
                    {
                        try
                        {
                            player.Photo = _webApiService.GetStream(photoUrl)?.ReadImage();
                        }
                        catch (Exception)
                        {
                            _logger.Warning($"Photo from url '{photoUrl}' not found.");
                        }
                    }

                    // Category and Gender
                    if (player.Birthdate.HasValue)
                        squadPlayer.Category = Category.FromBirthdate(player.Birthdate.Value);
                    player.Gender = GenderType.Male;

                    // Position
                    var position = item?["position"]?.ToString();

                    if (!string.IsNullOrEmpty(position))
                    {
                        var allowedPositionTypes = new List<PositionType>();
                        switch (position)
                        {
                            case "Goalkeeper":
                                allowedPositionTypes.Add(PositionType.GoalKeeper);
                                break;
                            case "Defender":
                                allowedPositionTypes.AddRange([PositionType.Defender]);
                                break;
                            case "Midfielder":
                                allowedPositionTypes.AddRange([PositionType.Midfielder]);
                                break;
                            case "Attacker":
                                allowedPositionTypes.AddRange([PositionType.Forward]);
                                break;
                            default:
                                break;
                        }
                        var allowedPositions = Position.GetPlayerPositions().Where(x => allowedPositionTypes.Contains(x.Type)).ToList();

                        if (allowedPositions.Count != 0)
                        {
                            var naturalPosition = new RatedPosition(RandomGenerator.ListItem(allowedPositions), PositionRating.Natural)
                            {
                                IsNatural = true
                            };
                            player.Positions.ToList().ForEach(x => player.RemovePosition(x.Position));
                            player.AddPosition(naturalPosition);

                            allowedPositions.Remove(naturalPosition.Position);

                            if (allowedPositions.Count != 0)
                                EnumerableHelper.Iteration(RandomGenerator.Int(0, 2), _ =>
                                {
                                    var position = RandomGenerator.ListItem(allowedPositions);
                                    if (!player.Positions.Any(x => x.Position == position))
                                        player.AddPosition(position, RandomGenerator.Enum<PositionRating>());
                                });
                        }

                        project.AddPlayer(squadPlayer);
                        index++;

                        step?.UpdateProgress((double)index / countPlayers);
                    }
                }
            }
        }

        public void Dispose() => _webApiService.Dispose();
    }
}
