// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Application.Contracts
{
    public interface IReadService
    {
        bool CanRead(string filename);

        Task<IProject> ReadAsync(string filename, CancellationToken? token = null);

        Task<byte[]?> ReadImageAsync(string filename, CancellationToken? token = null);

        Task<IEnumerable<Team>> ReadTeamsAsync(string filename, CancellationToken? token = null);

        Task<IEnumerable<Stadium>> ReadStadiumsAsync(string filename, CancellationToken? token = null);
    }
}
