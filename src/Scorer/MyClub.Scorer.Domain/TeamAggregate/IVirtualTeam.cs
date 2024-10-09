// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Domain;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.TeamAggregate
{
    public interface IVirtualTeam : IEntity, ISimilar
    {
        Team? GetTeam();
    }
}
