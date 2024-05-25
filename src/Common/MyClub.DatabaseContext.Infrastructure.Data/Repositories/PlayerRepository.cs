// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.EntityFrameworkCore;
using MyClub.DatabaseContext.Domain.PlayerAggregate;

namespace MyClub.DatabaseContext.Infrastructure.Data.Repositories
{
    public class PlayerRepository(MyClubContext dbContext) : GenericRepository<Player>(dbContext), IPlayerRepository
    {
        public override IQueryable<Player> GetAll() => base.GetAll().Include(x => x.Phones).Include(x => x.Emails).Include(x => x.Injuries).Include(x => x.Positions);
    }
}
