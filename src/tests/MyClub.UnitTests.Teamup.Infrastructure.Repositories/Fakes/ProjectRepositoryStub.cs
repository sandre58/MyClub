// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.ClubAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.Randomize;
using MyClub.Teamup.Domain.SeasonAggregate;
using MyNet.Utilities;

namespace MyClub.UnitTests.Teamup.Infrastructure.Repositories.Fakes
{
    public class ProjectRepositoryStub : IProjectRepository
    {
        private readonly Project _project;

        public ProjectRepositoryStub()
        {
            _project = new Project("Project", new Club("Club"), Category.Adult, Season.CurrentYear, "color");
            PlayerRandomFactory.RandomSquadPlayers(min: 20, max: 30).ForEach(x => _project.AddPlayer(x));
        }

        public void Load(Project project) => throw new NotImplementedException();
        public void Clear() => throw new NotImplementedException();
        public void Save() => throw new NotImplementedException();
        public bool HasCurrent() => throw new NotImplementedException();
        public Project GetCurrentOrThrow() => _project;
        public void Update(Action<Project> action) => throw new NotImplementedException();
    }
}
