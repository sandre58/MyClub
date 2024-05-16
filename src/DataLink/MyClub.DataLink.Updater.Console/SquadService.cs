// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.DatabaseContext.Domain.PlayerAggregate;
using MyNet.Http;
using Newtonsoft.Json.Linq;

namespace MyClub.DataLink.Updater.Console
{
    internal class SquadService(WebApiService webApiService) : WebEntityService<Player>(webApiService, "https://v3.football.api-sports.io/players/squads")
    {
        protected override IEnumerable<Player> GetEntities(JToken jtoken, IDictionary<string, string> parameters)
        {
            var players = jtoken?.First?["players"];
            var team = jtoken?.First?["team"];

            if (players is not null)
                foreach (var item in players)
                {
                    if (item is not null && team is not null)
                    {
                        var name = item["name"]?.ToString();
                        var teamId = team["id"]?.ToString();

                        if (!string.IsNullOrEmpty(name))
                        {
                            var result = new Player(string.Empty, string.Empty)
                            {
                                Description = $"id={item["id"]?.ToString()},team={teamId}",
                            };

                            yield return result;
                        }
                    }
                }
        }
    }
}
