// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using MyClub.Scorer.Domain.ProjectAggregate;

namespace MyClub.Scorer.Application.Contracts
{
    public interface IWriteService
    {
        Task<bool> WriteAsync(IProject project, string filename, CancellationToken? token = null);
    }
}
