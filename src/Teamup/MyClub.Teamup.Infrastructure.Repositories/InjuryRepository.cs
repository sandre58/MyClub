// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Services;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Infrastructure.Repositories
{
    public class InjuryRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<Injury>(projectRepository, auditService), IInjuryRepository
    {
        public override IEnumerable<Injury> GetAll() => CurrentProject.Players.SelectMany(x => x.Player.Injuries);

        public Injury Insert(Player player, DateTime date, string condition, InjurySeverity severity)
        {
            var added = player.AddInjury(date, condition, severity);

            AuditCreatedItem(added);

            return added;
        }

        protected override Injury AddCore(Injury item) => item;

        protected override bool DeleteCore(Injury item) => CurrentProject.Players.Any(x => x.Player.RemoveInjury(item.Id));
    }
}
