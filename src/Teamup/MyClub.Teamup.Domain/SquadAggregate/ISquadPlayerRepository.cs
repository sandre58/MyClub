// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain;

namespace MyClub.Teamup.Domain.SquadAggregate
{
    public interface ISquadPlayerRepository : IRepository<SquadPlayer>
    {
        SquadPlayer GetByPlayerId(Guid playerId);
    }
}
