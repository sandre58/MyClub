// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Teamup.Domain.HolidaysAggregate;
using MyClub.Domain.Services;
using MyClub.Teamup.Domain.ProjectAggregate;
using System.Linq;

namespace MyClub.Teamup.Infrastructure.Repositories
{
    public class HolidaysRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<Holidays>(projectRepository, auditService), IHolidaysRepository
    {
        public override IEnumerable<Holidays> GetAll() => CurrentProject.Holidays;

        protected override Holidays AddCore(Holidays item) => CurrentProject.AddHolidays(item);

        protected override IEnumerable<Holidays> AddRangeCore(IEnumerable<Holidays> items) => items.Select(AddCore);

        protected override bool RemoveCore(Holidays item) => CurrentProject.RemoveHolidays(item);

        protected override int RemoveRangeCore(IEnumerable<Holidays> items) => items.Count(RemoveCore);
    }
}
