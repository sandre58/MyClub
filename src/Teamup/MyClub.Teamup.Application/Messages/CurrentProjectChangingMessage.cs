// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Teamup.Domain.ProjectAggregate;

namespace MyClub.Teamup.Application.Messages
{
    public class CurrentProjectChangingMessage
    {
        public Project? Project { get; }

        public string? Filename { get; }

        public CurrentProjectChangingMessage(Project? project = null, string? filename = null) => (Project, Filename) = (project, filename);
    }
}
