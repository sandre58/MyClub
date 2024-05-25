// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Linq;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Plugins.Contracts;
using MyClub.Scorer.Wpf.Services;
using MyNet.UI.ViewModels.Import;

namespace MyClub.Scorer.Wpf.ViewModels.Import
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
