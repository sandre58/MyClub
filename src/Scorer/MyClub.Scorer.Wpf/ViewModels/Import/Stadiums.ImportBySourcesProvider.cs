// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Linq;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Plugins.Contracts;
using MyClub.Scorer.Wpf.Services;
using MyNet.UI.ViewModels.Import;

namespace MyClub.Scorer.Wpf.ViewModels.Import
{
    internal class StadiumsImportBySourcesProvider : ItemsBySourceProvider<StadiumImportableViewModel>
    {
        public StadiumsImportBySourcesProvider(PluginsService pluginsService, StadiumService stadiumService)
            : base(pluginsService.GetPlugins<IImportStadiumsSourcePlugin>()
                                 .Select(x => (IImportSourceViewModel<StadiumImportableViewModel>)new StadiumsImportSourceViewModel(x, stadiumService))
                                 .ToList())
        { }
    }
}
