// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Teamup.Domain.HolidaysAggregate;
using MyClub.Domain.Services;
using MyClub.Teamup.Domain.ProjectAggregate;

namespace MyClub.Teamup.Infrastructure.Repositories
{
    public class HolidaysRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<Holidays>(projectRepository, auditService), IHolidaysRepository
    {
        public override IEnumerable<Holidays> GetAll() => CurrentProject.Holidays;

        protected override Holidays AddCore(Holidays item) => CurrentProject.AddHolidays(item);

        protected override bool DeleteCore(Holidays item) => CurrentProject.RemoveHolidays(item);
    }
}
