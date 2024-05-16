// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Domain;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.TeamAggregate
{
    public interface ITeam : IEntity, ISimilar
    {
        string Name { get; }

        string ShortName { get; }

        public byte[]? Logo { get; }

        public string? HomeColor { get; }

        public string? AwayColor { get; }
    }
}
