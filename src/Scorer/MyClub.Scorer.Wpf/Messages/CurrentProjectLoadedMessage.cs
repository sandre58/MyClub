// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Domain.ProjectAggregate;

namespace MyClub.Scorer.Application.Messages
{
    public class CurrentProjectLoadedMessage
    {
        public IProject Project { get; }

        public string? Filename { get; }

        public CurrentProjectLoadedMessage(IProject project, string? filename = null) => (Project, Filename) = (project, filename);
    }
}
