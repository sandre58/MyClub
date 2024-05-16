// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.EntityFrameworkCore;
using MyClub.DatabaseContext.Domain.ClubAggregate;

namespace MyClub.DatabaseContext.Infrastructure.Data.Repositories
{
    public class TeamRepository(MyTeamup dbContext) : GenericRepository<Team>(dbContext), ITeamRepository
    {
        public override IQueryable<Team> GetAll() => base.GetAll().Include(x => x.Stadium).Include(x => x.Club).ThenInclude(x => x.Stadium);
    }
}
