// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.EntityFrameworkCore;
using MyClub.DatabaseContext.Domain.ClubAggregate;

namespace MyClub.DatabaseContext.Infrastructure.Data.Repositories
{
    public class ClubRepository(MyTeamup dbContext) : GenericRepository<Club>(dbContext), IClubRepository
    {
        public override IQueryable<Club> GetAll() => base.GetAll().Include(x => x.Stadium).Include(x => x.Teams).ThenInclude(x => x.Stadium);
    }
}
