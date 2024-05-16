// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Scorer.Domain.CompetitionAggregate;

namespace MyClub.Scorer.Domain.ProjectAggregate
{
    public interface IProjectRepository
    {
        bool HasCurrent();

        IProject GetCurrentOrThrow();

        ICompetition GetCompetition();

        void Load(IProject project);

        void Clear();

        void Update(Action<IProject> action);

        void Save();
    }
}
