// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.DatabaseContext.Domain.CompetitionAggregate;

namespace MyClub.DatabaseContext.Infrastructure.Data.Repositories
{
    public class CompetitionRepository(MyClubContext dbContext) : GenericRepository<Competition>(dbContext), ICompetitionRepository
    {
    }
}
