// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Linq;
using MyClub.Teamup.Domain.ProjectAggregate;

namespace MyClub.Teamup.Domain.MyTeamAggregate
{
    public class MyTeamDomainService(IProjectRepository projectRepository) : IMyTeamDomainService
    {
        private readonly IProjectRepository _projectRepository = projectRepository;

        public bool ValidateOrder()
        {
            var orders = _projectRepository.GetCurrentOrThrow().Club.Teams.Select(x => x.Order).ToList();
            return orders.Distinct().Count() == orders.Count;
        }
    }
}
