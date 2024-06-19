// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Services;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.Factories
{
    public class ProjectFactory(IAuditService auditService) : IProjectFactory
    {
        private readonly IAuditService _auditService = auditService;

        public Task<LeagueProject> CreateLeagueAsync(CancellationToken cancellationToken = default)
        {
            var result = new LeagueProject(MyClubResources.League)
            {
                SchedulingParameters = new SchedulingParameters(DateTime.Today, DateTime.Today.AddMonths(10), 15.Hours(), 2.Days(), 3.Days(), true)
            };
            _auditService.New(result);
            return Task.FromResult(result);
        }

        public Task<CupProject> CreateCupAsync(CancellationToken cancellationToken = default)
        {
            var result = new CupProject(MyClubResources.Cup)
            {
                DefaultSchedulingParameters = new SchedulingParameters(DateTime.Today, DateTime.Today.AddMonths(10), 15.Hours(), 2.Days(), 3.Days(), true)
            };
            _auditService.New(result);
            return Task.FromResult(result);
        }

        public Task<TournamentProject> CreateTournamentAsync(CancellationToken cancellationToken = default)
        {
            var result = new TournamentProject(MyClubResources.Cup)
            {
                DefaultSchedulingParameters = new SchedulingParameters(DateTime.Today, DateTime.Today.AddMonths(10), 15.Hours(), 2.Days(), 3.Days(), true)
            };
            _auditService.New(result);
            return Task.FromResult(result);
        }
    }
}
