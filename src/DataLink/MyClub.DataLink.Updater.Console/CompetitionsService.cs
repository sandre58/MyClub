// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MyClub.DatabaseContext.Domain.CompetitionAggregate;
using MyNet.Http;
using MyNet.Utilities;
using MyNet.Utilities.Extensions;
using Newtonsoft.Json.Linq;

namespace MyClub.DataLink.Updater.Console
{
    internal class CompetitionsService(WebApiService webApiService) : WebEntityService<Competition>(webApiService, "https://v3.football.api-sports.io/leagues")
    {
        private static readonly IEnumerable<string> ExcludeNames = [" - ", "U17", "U18", "U19", "U20", "U21", "U22", "U23", "Friendlies"];

        protected override IEnumerable<Competition> GetEntities(JToken jtoken, IDictionary<string, string> parameters)
        {
            foreach (var item in jtoken)
            {
                var league = item?["league"];
                var country = item?["country"];
                var season = item?["seasons"]?.First;

                if (league is null || country is null || season is null) continue;

                var name = league?["name"]?.ToString();
                var shortName = league?["name"]?.ToString();
                var type = league?["type"]?.ToString();
                var logoUrl = league?["logo"]?.ToString();
                var isLeague = type == Competition.League;

                if (!string.IsNullOrEmpty(name)
                    && !string.IsNullOrEmpty(shortName)
                    && !string.IsNullOrEmpty(type)
                    && !string.IsNullOrEmpty(logoUrl)
                    && !ExcludeNames.Any(name.Contains))
                {
                    var id = league?["id"]?.ToString();
                    var countryName = country?["name"]?.ToString();
                    var startDate = season?["start"]?.ToString();
                    var endDate = season?["end"]?.ToString();
                    byte[]? logo = null;

                    try
                    {
                        logo = WebApiService.GetStream(logoUrl)?.ReadImage();
                    }
                    catch (Exception)
                    {
                        // Empty
                    }

                    yield return new Competition(name, shortName, type)
                    {
                        IsNational = countryName == "World",
                        Country = ClubsService.SpecificCountryAssociation.GetOrDefault(countryName.OrEmpty()) ?? countryName,
                        Category = "Adult",
                        Logo = logo,
                        ExtraTimeDuration = isLeague ? null : TimeSpan.FromMinutes(15),
                        ExtraTimeNumber = isLeague ? null : 2,
                        NumberOfPenaltyShootouts = isLeague ? null : 5,
                        PointsByGamesDrawn = isLeague ? 1 : null,
                        PointsByGamesLost = isLeague ? 0 : null,
                        PointsByGamesWon = isLeague ? 3 : null,
                        RankLabels = isLeague ? "1,1,Champion,CH,#008000," : null,
                        RegulationTimeDuration = TimeSpan.FromMinutes(45),
                        RegulationTimeNumber = 2,
                        SortingColumns = isLeague ? "Points,GoalDifference,GoalsFor" : null,
                        Description = $"id={id}",
                        StartDate = DateTime.TryParse(startDate, CultureInfo.InvariantCulture, out var startDateResult) ? startDateResult : null,
                        EndDate = DateTime.TryParse(endDate, CultureInfo.InvariantCulture, out var endDateResult) ? endDateResult : null,
                    };
                }
            }
        }
    }
}
