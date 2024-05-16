// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using MyClub.DatabaseContext.Domain.PlayerAggregate;
using MyNet.Http;
using MyNet.Utilities;
using MyNet.Utilities.Extensions;
using Newtonsoft.Json.Linq;

namespace MyClub.DataLink.Updater.Console
{
    internal class PlayersService(WebApiService webApiService) : WebEntityService<Player>(webApiService, "https://v3.football.api-sports.io/players")
    {
        protected override IEnumerable<Player> GetEntities(JToken jtoken, IDictionary<string, string> parameters)
        {
            foreach (var item in jtoken)
            {
                var player = item?["player"];
                var statistics = item?["statistics"];
                var team = statistics?.First?["team"];
                var games = statistics?.First?["games"];

                if (player is not null && team is not null)
                {
                    var photoUrl = player?["photo"]?.ToString();
                    var firstName = player?["firstname"]?.ToString();
                    var lastName = player?["lastname"]?.ToString();
                    var nationality = player?["nationality"]?.ToString();
                    var birth = player?["birth"];
                    var placeOfBirth = birth?["place"]?.ToString();
                    var teamId = team?["id"]?.ToString();
                    var position = games?["position"]?.ToString();

                    if (!string.IsNullOrEmpty(firstName)
                        && !string.IsNullOrEmpty(lastName)
                        && !string.IsNullOrEmpty(photoUrl))
                    {
                        byte[]? photo = null;

                        try
                        {
                            photo = WebApiService.GetStream(photoUrl)?.ReadImage();
                        }
                        catch (Exception)
                        {
                            // Empty
                        }

                        var result = new Player(firstName, lastName)
                        {
                            Birthdate = DateTime.TryParse(birth?["date"]?.ToString(), CultureInfo.InvariantCulture, out var birthdate) ? birthdate : null,
                            Height = int.TryParse(player?["height"]?.ToString().Split(" ")[0], out var height) ? height : null,
                            Weight = int.TryParse(player?["weight"]?.ToString().Split(" ")[0], out var weight) ? weight : null,
                            PlaceOfBirth = placeOfBirth,
                            Photo = photo,
                            Country = ClubsService.SpecificCountryAssociation.GetOrDefault(nationality.OrEmpty()) ?? nationality,
                            Category = "Adult",
                            Description = $"id={player?["id"]?.ToString()},team={teamId}",
                            Gender = "Male",
                            Laterality = "RightHander"
                        };

                        if (!string.IsNullOrEmpty(position))
                        {
                            var pos = position switch
                            {
                                "Goalkeeper" => "GoalKeeper",
                                "Defender" => "CenterBack",
                                "Midfielder" => "CenterMidfielder",
                                _ => "Forward",
                            };

                            result.Positions =
                            [
                                new RatedPosition
                                {
                                    IsNatural = true,
                                    Player = result,
                                    Position = pos,
                                    Rating = "Natural"
                                }
                            ];
                        }

                        yield return result;
                    }
                }
            }
        }
    }
}
