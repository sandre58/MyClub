// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain.Services;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Infrastructure.Repositories
{
    public class ProjectRepository(IAuditService auditService) : IProjectRepository
    {
        private IProject? _currentProject;
        private readonly IAuditService _auditService = auditService;

        public bool HasCurrent() => _currentProject is not null;

        public IProject GetCurrentOrThrow() => _currentProject.OrThrow();

        public ICompetition GetCompetition() => GetCurrentOrThrow().Competition;

        public void Load(IProject project) => _currentProject = project;

        public void Clear() => _currentProject = null;

        public void Save() => _auditService.Update(GetCurrentOrThrow());

        public void Update(Action<IProject> action)
        {
            if (_currentProject is null) return;

            action(_currentProject);
        }
    }
}
