// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Linq;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Plugins.Contracts;
using MyClub.Teamup.Wpf.Services;
using MyNet.UI.ViewModels.Import;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class CompetitionsImportBySourcesProvider : ItemsBySourceProvider<CompetitionImportableViewModel>
    {
        public CompetitionsImportBySourcesProvider(PluginsService pluginsService, CompetitionService competitionService)
            : base(pluginsService.GetPlugins<IImportCompetitionsSourcePlugin>()
                                 .Select(x => (IImportSourceViewModel<CompetitionImportableViewModel>)new CompetitionsImportSourceViewModel(x, competitionService))
                                 .ToList())
        { }
    }
}
