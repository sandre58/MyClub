// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace MyClub.Scorer.Domain.ProjectAggregate
{
    public interface IProjectFactory
    {
        Task<LeagueProject> CreateLeagueAsync(CancellationToken cancellationToken = default);

        Task<CupProject> CreateCupAsync(CancellationToken cancellationToken = default);

        Task<TournamentProject> CreateTournamentAsync(CancellationToken cancellationToken = default);
    }
}
