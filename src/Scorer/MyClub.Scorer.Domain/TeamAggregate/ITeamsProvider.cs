// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace MyClub.Scorer.Domain.TeamAggregate
{
    public interface ITeamsProvider
    {
        IEnumerable<IVirtualTeam> ProvideTeams();
    }
}
