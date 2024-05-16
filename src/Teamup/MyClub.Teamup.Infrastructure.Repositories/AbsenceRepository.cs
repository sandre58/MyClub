// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Services;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;

namespace MyClub.Teamup.Infrastructure.Repositories
{
    public class AbsenceRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<Absence>(projectRepository, auditService), IAbsenceRepository
    {
        public override IEnumerable<Absence> GetAll() => CurrentProject.Players.SelectMany(x => x.Player.Absences);

        public Absence Insert(Player player, DateTime startDate, DateTime endDate, string label)
        {
            var added = player.AddAbsence(startDate, endDate, label);

            AuditCreatedItem(added);

            return added;
        }

        protected override Absence AddCore(Absence item) => item;

        protected override bool DeleteCore(Absence item) => CurrentProject.Players.Any(x => x.Player.RemoveAbsence(item.Id));
    }
}
