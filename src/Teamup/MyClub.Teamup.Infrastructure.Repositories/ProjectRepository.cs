// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain.Services;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyNet.Utilities;

namespace MyClub.Teamup.Application.Services
{
    public class ProjectRepository(IAuditService auditService) : IProjectRepository
    {
        private Project? _currentProject;
        private readonly IAuditService _auditService = auditService;

        public bool HasCurrent() => _currentProject is not null;

        public Project GetCurrentOrThrow() => _currentProject.OrThrow();

        public void Load(Project project) => _currentProject = project;

        public void Clear() => _currentProject = null;

        public void Save() => _auditService.Update(GetCurrentOrThrow());

        public void Update(Action<Project> action)
        {
            if (_currentProject is null) return;

            action(_currentProject);
        }
    }
}
