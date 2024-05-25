// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Linq;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Plugins.Contracts;
using MyClub.Teamup.Wpf.Services;
using MyNet.UI.ViewModels.Import;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class TeamsImportBySourcesProvider : ItemsBySourceProvider<TeamImportableViewModel>
    {
        public TeamsImportBySourcesProvider(PluginsService pluginsService, TeamService teamService)
            : base(pluginsService.GetPlugins<IImportTeamsSourcePlugin>()
                                 .Select(x => (IImportSourceViewModel<TeamImportableViewModel>)new TeamsImportSourceViewModel(x, teamService))
                                 .ToList())
        { }
    }
}
