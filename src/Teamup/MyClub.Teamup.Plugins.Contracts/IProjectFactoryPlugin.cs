// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Plugins.Contracts.Base;

namespace MyClub.Teamup.Plugins.Contracts
{
    public interface IProjectFactoryPlugin : IProjectFactory, IPlugin
    {
    }
}
