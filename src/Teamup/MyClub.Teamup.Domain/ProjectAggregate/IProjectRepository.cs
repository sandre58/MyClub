// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;

namespace MyClub.Teamup.Domain.ProjectAggregate
{
    public interface IProjectRepository
    {
        bool HasCurrent();

        Project GetCurrentOrThrow();

        void Load(Project project);

        void Clear();

        void Update(Action<Project> action);

        void Save();
    }
}
