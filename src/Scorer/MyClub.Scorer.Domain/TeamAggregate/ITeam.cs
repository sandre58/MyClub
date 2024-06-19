// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Domain;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.TeamAggregate
{
    public interface ITeam : IEntity, ISimilar
    {
        string Name { get; }

        string ShortName { get; }

        byte[]? Logo { get; }

        string? HomeColor { get; }

        string? AwayColor { get; }

        Stadium? Stadium { get; }
    }
}
