// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Teamup.Domain.ProjectAggregate;

namespace MyClub.Teamup.Application.Messages
{
    public class CurrentProjectChangedMessage
    {
        public Project? CurrentProject { get; }

        public string? Filename { get; }

        public CurrentProjectChangedMessage(Project? project = null, string? filename = null) => (CurrentProject, Filename) = (project, filename);
    }
}
