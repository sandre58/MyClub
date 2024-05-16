// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.DatabaseContext.Domain.ClubAggregate;
using MyClub.DatabaseContext.Domain.StadiumAggregate;
using MyNet.Http;
using MyNet.Utilities;
using MyNet.Utilities.Extensions;
using Newtonsoft.Json.Linq;

namespace MyClub.DataLink.Updater.Console
{
    internal class ClubsService(WebApiService webApiService) : WebEntityService<Club>(webApiService, "https://v3.football.api-sports.io/teams")
    {
        private static readonly IEnumerable<string> ExcludeNames = ["U18", "U19", "U20", "U21"];
        public static readonly IDictionary<string, string> SpecificCountryAssociation = new Dictionary<string, string>
        {
            { "England", "UnitedKingdomOfGreatBritainAndNorthernIreland" },
            { "Costa-Rica", "CostaRica" },
            { "Saudi-Arabia", "SaudiArabia" },
            { "South-Korea", "SouthKorea" },
            { "USA", "UnitedStatesOfAmerica" },
            { "Wales", "UnitedKingdomOfGreatBritainAndNorthernIreland" }
        };

        protected override IEnumerable<Club> GetEntities(JToken jtoken, IDictionary<string, string> parameters)
        {
            foreach (var item in jtoken)
            {
                var team = item?["team"];
                var stadium = item?["venue"];

                if (team is null || stadium is null) continue;

                var name = team?["name"]?.ToString();
                var shortName = team?["code"]?.ToString();
                var stadiumName = stadium?["name"]?.ToString();
                var logoUrl = team?["logo"]?.ToString();

                if (!string.IsNullOrEmpty(name)
                    && !string.IsNullOrEmpty(shortName)
                    && !string.IsNullOrEmpty(stadiumName)
                    && !string.IsNullOrEmpty(logoUrl)
                    && !ExcludeNames.Any(name.Contains))
                {
                    var id = team?["id"]?.ToString();
                    var isNational = team?["national"]?.ToString();
                    var country = team?["country"]?.ToString();
                    var stadiumGround = stadium?["surface"]?.ToString();
                    var stadiumAddress = stadium?["address"]?.ToString();
                    var stadiumCity = stadium?["city"]?.ToString();
                    byte[]? logo = null;

                    try
                    {
                        logo = WebApiService.GetStream(logoUrl)?.ReadImage();
                    }
                    catch (Exception)
                    {
                        // Empty
                    }

                    var club = new Club(name)
                    {
                        IsNational = bool.TryParse(isNational, out var boolean) && boolean,
                        Country = SpecificCountryAssociation.GetOrDefault(country.OrEmpty()) ?? country,
                        Logo = logo,
                        Description = $"id={id},league={parameters.GetOrDefault("league")}",
                        Stadium = !string.IsNullOrEmpty(stadiumName)
                                  ? new Stadium(stadiumName, stadiumGround.OrEmpty())
                                  {
                                      Street = stadiumAddress,
                                      City = stadiumCity,
                                      Country = SpecificCountryAssociation.GetOrDefault(country.OrEmpty()) ?? country
                                  }
                                  : null
                    };
                    var teamEntity = new Team(name, shortName)
                    {
                        Club = club,
                        Category = "Adult",
                        Stadium = club.Stadium
                    };
                    club.Teams.Add(teamEntity);

                    yield return club;
                }
            }
        }
    }
}
