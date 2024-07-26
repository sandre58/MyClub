// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Services;
using MyClub.Scorer.Domain.ProjectAggregate;

namespace MyClub.Scorer.Domain.Factories
{
    public class ProjectFactory(IAuditService auditService) : IProjectFactory
    {
        private readonly IAuditService _auditService = auditService;

        public Task<LeagueProject> CreateLeagueAsync(CancellationToken cancellationToken = default)
        {
            var result = new LeagueProject(MyClubResources.League);
            _auditService.New(result);
            return Task.FromResult(result);
        }

        public Task<CupProject> CreateCupAsync(CancellationToken cancellationToken = default)
        {
            var result = new CupProject(MyClubResources.Cup);
            _auditService.New(result);
            return Task.FromResult(result);
        }

        public Task<TournamentProject> CreateTournamentAsync(CancellationToken cancellationToken = default)
        {
            var result = new TournamentProject(MyClubResources.Cup);
            _auditService.New(result);
            return Task.FromResult(result);
        }
    }
}
