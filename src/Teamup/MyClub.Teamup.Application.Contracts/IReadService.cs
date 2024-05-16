// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Domain.SquadAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.TeamAggregate;

namespace MyClub.Teamup.Application.Contracts
{
    public interface IReadService
    {
        bool CanRead(string filename);

        Task<Project> ReadAsync(string filename, CancellationToken? token = null);

        Task<byte[]?> ReadImageAsync(string filename, CancellationToken? token = null);

        Task<IEnumerable<SquadPlayer>> ReadSquadPlayersAsync(string filename, CancellationToken? token = null);

        Task<IEnumerable<Team>> ReadTeamsAsync(string filename, CancellationToken? token = null);

        Task<IEnumerable<ICompetition>> ReadCompetitionsAsync(string filename, CancellationToken? token = null);
    }
}
