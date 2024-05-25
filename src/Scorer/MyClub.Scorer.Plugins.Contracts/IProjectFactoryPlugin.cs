// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Plugins.Contracts.Base;

namespace MyClub.Scorer.Plugins.Contracts
{
    public interface IProjectFactoryPlugin : IProjectFactory, IPlugin
    {
    }
}
