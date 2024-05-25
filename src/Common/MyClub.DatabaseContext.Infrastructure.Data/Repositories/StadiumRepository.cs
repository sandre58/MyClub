// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.DatabaseContext.Domain.StadiumAggregate;

namespace MyClub.DatabaseContext.Infrastructure.Data.Repositories
{
    public class StadiumRepository(MyClubContext dbContext) : GenericRepository<Stadium>(dbContext), IStadiumRepository
    {
    }
}
