// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Services;
using MyClub.Teamup.Domain.SquadAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;

namespace MyClub.Teamup.Infrastructure.Repositories
{
    public class SquadPlayerRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<SquadPlayer>(projectRepository, auditService), ISquadPlayerRepository
    {
        public override IEnumerable<SquadPlayer> GetAll() => CurrentProject.Players;

        public SquadPlayer GetByPlayerId(Guid playerId) => GetAll().First(x => x.Player.Id == playerId);

        protected override SquadPlayer AddCore(SquadPlayer item) => CurrentProject.AddPlayer(item);

        protected override bool DeleteCore(SquadPlayer item) => CurrentProject.RemovePlayer(item);
    }
}
