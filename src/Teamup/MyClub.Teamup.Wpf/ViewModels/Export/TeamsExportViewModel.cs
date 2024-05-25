// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.Services.Providers.Base;
using MyClub.Teamup.Wpf.Settings;
using MyClub.Teamup.Wpf.ViewModels.Edition;
using MyNet.Humanizer;
using MyNet.UI.ViewModels.Export;
using MyNet.Utilities.IO.FileExtensions;

namespace MyClub.Teamup.Wpf.ViewModels.Export
{
    internal class TeamsExportViewModel(ProjectInfoProvider projectInfoProvider)
        : FileExportByColumnsViewModelBase<EditableTeamViewModel, ColumnMappingWrapper<TeamExportDto, object?>>(FileExtensionInfoProvider.Excel.Concat(FileExtensionInfoProvider.Csv),
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

        protected override Task<bool> ExportItemsAsync(IEnumerable<EditableTeamViewModel> items) => Task.FromResult(true);
    }
}
