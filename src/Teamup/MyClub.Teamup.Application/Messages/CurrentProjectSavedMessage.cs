// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Teamup.Domain.ProjectAggregate;

namespace MyClub.Teamup.Application.Messages
{
    public class CurrentProjectSavedMessage
    {
        public Project Project { get; }

        public string FilePath { get; }

        public CurrentProjectSavedMessage(Project project, string filePath) => (Project, FilePath) = (project, filePath);
    }
}
