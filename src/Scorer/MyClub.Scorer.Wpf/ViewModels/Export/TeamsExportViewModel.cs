// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.Services.Providers.Base;
using MyClub.Scorer.Wpf.Settings;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Humanizer;
using MyNet.UI.ViewModels.Export;
using MyNet.Utilities.IO.FileExtensions;

namespace MyClub.Scorer.Wpf.ViewModels.Export
{
    internal class TeamsExportViewModel(ProjectInfoProvider projectInfoProvider)
        : FileExportByColumnsViewModelBase<TeamViewModel, ColumnMappingWrapper<TeamDto, object?>>(FileExtensionInfoProvider.Excel.Concat(FileExtensionInfoProvider.Csv),
               () => projectInfoProvider.ProvideExportName(MyClubResources.Teams),
               new TeamColumnsExportProvider().ProvideWrappers(),
               defaultFolder: ExportTeamsSettings.Default.Folder)
    {
        protected override void SaveConfiguration()
        {
            ExportTeamsSettings.Default.ColumnsOrder = Columns.Select(x => x.Item.Key).Humanize(";");
            ExportTeamsSettings.Default.SelectedColumns = Columns.Where(x => x.IsSelected).Select(x => x.Item.Key).Humanize(";");
            ExportTeamsSettings.Default.Folder = Path.GetDirectoryName(Destination);

            ExportTeamsSettings.Default.Save();
        }

        protected override Task<bool> ExportItemsAsync(IEnumerable<TeamViewModel> items) => Task.FromResult(true);
    }
}
