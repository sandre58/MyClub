// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using MyNet.Humanizer;
using MyNet.Utilities.IO.FileExtensions;
using MyClub.Teamup.Application.Dtos;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.Settings;
using MyClub.Teamup.Wpf.ViewModels.Edition;

namespace MyClub.Teamup.Wpf.ViewModels.Export
{
    internal class TeamsExportViewModel(ProjectInfoProvider projectInfoProvider) : ExportViewModel<EditableTeamViewModel, TeamExportDto>(FileExtensionInfoProvider.Excel.Concat(FileExtensionInfoProvider.Csv),
               () => projectInfoProvider.ProvideExportName(MyClubResources.Teams),
               new TeamColumnsExportProvider(),
               defaultFolder: ExportTeamsSettings.Default.Folder)
    {
        protected override void Save()
        {
            ExportTeamsSettings.Default.ColumnsOrder = Columns.Select(x => x.Item.ResourceKey).Humanize(";");
            ExportTeamsSettings.Default.SelectedColumns = Columns.Where(x => x.IsSelected).Select(x => x.Item.ResourceKey).Humanize(";");
            ExportTeamsSettings.Default.Folder = Path.GetDirectoryName(Destination);

            ExportTeamsSettings.Default.Save();
        }
    }
}
