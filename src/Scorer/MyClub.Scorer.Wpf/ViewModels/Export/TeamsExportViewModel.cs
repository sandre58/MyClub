// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using MyNet.Humanizer;
using MyNet.Utilities.IO.FileExtensions;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.Settings;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Application.Dtos;

namespace MyClub.Scorer.Wpf.ViewModels.Export
{
    internal class TeamsExportViewModel(ProjectInfoProvider projectInfoProvider) : ExportViewModel<TeamViewModel, TeamDto>(FileExtensionInfoProvider.Excel.Concat(FileExtensionInfoProvider.Csv),
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
