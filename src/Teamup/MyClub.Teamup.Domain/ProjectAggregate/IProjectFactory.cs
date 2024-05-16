// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace MyClub.Teamup.Domain.ProjectAggregate
{
    public interface IProjectFactory
    {
        Task<Project> CreateAsync(CancellationToken cancellationToken = default);
    }
}
