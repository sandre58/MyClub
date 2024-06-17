// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Services;
using MyClub.Scorer.Domain.PersonAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Infrastructure.Repositories
{
    public class PlayerRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<Player>(projectRepository, auditService), IPlayerRepository
    {
        public override IEnumerable<Player> GetAll() => CurrentProject.Teams.SelectMany(x => x.Players);

        public Player Insert(Team team, string firstName, string lastName)
        {
            var added = team.AddPlayer(firstName, lastName);

            AuditCreatedItem(added);

            return added;
        }

        protected override Player AddCore(Player item) => item;

        protected override IEnumerable<Player> AddRangeCore(IEnumerable<Player> items) => items;

        protected override bool RemoveCore(Player item) => CurrentProject.Teams.Any(x => x.RemovePlayer(item));

        protected override int RemoveRangeCore(IEnumerable<Player> items) => items.Count(RemoveCore);
    }
}
