// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using MyClub.Domain;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public interface IRound : IAuditableEntity, ICompetitionStage
    {
        Knockout Stage { get; }

        ReadOnlyObservableCollection<IVirtualTeam> Teams { get; }

        IVirtualTeam AddTeam(IVirtualTeam team);

        bool RemoveTeam(IVirtualTeam team);
    }
}
