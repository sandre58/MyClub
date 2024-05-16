// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Teamup.Domain.ProjectAggregate;

namespace MyClub.UnitTests.Teamup.Application.Fake
{
    internal class ProjectRepositoryMock : IProjectRepository
    {
        public void Clear() => throw new NotImplementedException();
        public Project GetCurrentOrThrow() => throw new NotImplementedException();
        public bool HasCurrent() => throw new NotImplementedException();
        public void Load(Project project) => throw new NotImplementedException();
        public void Save() => throw new NotImplementedException();
        public void Update(Action<Project> action) => throw new NotImplementedException();
    }
}
