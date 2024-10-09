// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.MatchAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Application.Services
{
    public class MatchStatisticsService(IMatchRepository matchRepository)
    {
        private readonly IMatchRepository _matchRepository = matchRepository;

        public List<PlayerStatisticsDto> GetPlayerStatistics()
        {
            var result = new Dictionary<Guid, PlayerStatisticsDto>();
            var matches = _matchRepository.GetAll();

            foreach (var match in matches)
            {
                //computeStatisticsForOpponent(match.Home);
                //computeStatisticsForOpponent(match.Away);
            }

            return [.. result.Values];

            //void addOrUpdate(Guid playerId, Guid teamId, Action<PlayerStatisticsDto> update)
            //{
            //    if (!result.TryGetValue(playerId, out var playerStatisticsDto))
            //    {
            //        playerStatisticsDto = new PlayerStatisticsDto() { TeamId = teamId };
            //        result.Add(playerId, playerStatisticsDto);
            //    }

            //    update(playerStatisticsDto);
            //}

            //void computeStatisticsForOpponent(MatchOpponent opponent)
            //{
            //    foreach (var goal in opponent.GetGoals())
            //    {
            //        goal.Scorer.IfNotNull(x => addOrUpdate(x.Id, opponent.Team.Id, x => x.Goals++));
            //        goal.Assist.IfNotNull(x => addOrUpdate(x.Id, opponent.Team.Id, x => x.Assists++));
            //    }

            //    foreach (var card in opponent.GetCards())
            //    {
            //        card.Player.IfNotNull(x => addOrUpdate(x.Id, opponent.Team.Id, x => x.Cards?.AddOrUpdate(card.Color, x.Cards.GetValueOrDefault(card.Color) + 1)));
            //    }
            //}
        }
    }
}
