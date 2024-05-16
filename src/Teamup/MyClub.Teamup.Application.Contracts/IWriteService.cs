// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using MyClub.Teamup.Domain.ProjectAggregate;

namespace MyClub.Teamup.Application.Contracts
{
    public interface IWriteService
    {
        Task<bool> WriteAsync(Project project, string filename, CancellationToken? token = null);
    }
}
