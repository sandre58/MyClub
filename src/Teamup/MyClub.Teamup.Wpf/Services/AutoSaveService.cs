// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DynamicData.Binding;
using MyClub.CrossCutting.FileSystem;
using MyClub.Teamup.Application.Contracts;
using MyClub.Teamup.Infrastructure.Packaging;
using MyClub.Teamup.Wpf.Services.Providers;
using MyNet.UI.Busy.Models;
using MyNet.UI.Services;
using MyNet.Utilities;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Helpers;
using MyNet.Utilities.IO.AutoSave;
using MyNet.Utilities.IO.FileHistory;
using MyNet.Utilities.Logging;

namespace MyClub.Teamup.Wpf.Services
{
    internal class AutoSaveService : AutoSaveServiceBase
    {
        private readonly IWriteService _writeService;
        private readonly ITempService _tempService;
        private readonly ProjectInfoProvider _projectInfoProvider;
        private readonly IRecentFileRepository _recentFileRepository;
        private string? _lastAutoSaveFilename;

        public AutoSaveService(ProjectInfoProvider projectInfoProvider, IWriteService writeService, ITempService tempService, IRecentFileRepository recentFileRepository, bool isEnabled = true, int intervalInSeconds = 300)
            : base(isEnabled, intervalInSeconds)
        {
            _projectInfoProvider = projectInfoProvider;
            _tempService = tempService;
            _writeService = writeService;
            _recentFileRepository = recentFileRepository;

            _projectInfoProvider.WhenPropertyChanged(x => x.IsDirty).Subscribe(x =>
                x.Value.IfFalse(() =>
                {
                    Stop();
                    Clean();
                }, Start));

            _projectInfoProvider.WhenProjectChanged(x =>
            {
                Clean();
                x.IfNull(Stop, _ => Start());
            });
        }

        protected override async Task<bool> SaveCoreAsync(CancellationToken? cancellationToken = null)
        {
            var currentProject = _projectInfoProvider.GetCurrentProject();
            if (currentProject is null || !_projectInfoProvider.IsDirty) return false;

            AppBusyManager.BackgroundBusyService.Wait<IndeterminateBusy>();

            try
            {
                _lastAutoSaveFilename = _tempService.GetFileName(TmprojFileExtensionInfo.TmprojExtension, _projectInfoProvider.Name);

                using (LogManager.MeasureTime($"Auto Save file : {_lastAutoSaveFilename}", TraceLevel.Debug))
                {
                    _ = await _writeService.WriteAsync(currentProject, _lastAutoSaveFilename, cancellationToken).ConfigureAwait(false);

                    // Add in registry
                    _ = _recentFileRepository.Add(new RecentFile(_projectInfoProvider.Name!, _lastAutoSaveFilename, null, null, false, true));
                }
            }
            finally
            {
                AppBusyManager.BackgroundBusyService.Resume();
            }

            return true;
        }

        public void Clean()
        {
            if (!string.IsNullOrEmpty(_lastAutoSaveFilename))
            {
                ForceDeleteFile(_lastAutoSaveFilename);
                _recentFileRepository.Remove(_lastAutoSaveFilename);
            }
            _lastAutoSaveFilename = null;
        }

        private static void ForceDeleteFile(string filename)
        {
            if (File.Exists(filename))
            {
                try
                {
                    FileHelper.RemoveFile(filename);
                }
                catch (FileAlreadyUsedException ex)
                {
                    LogManager.Warning($"Auto save failed : {ex.Message}");
                }
                catch (Exception)
                {
                    LogManager.Warning($"An error occured when attempting to delete autosave project {Path.GetFileName(filename)}");
                }
            }
        }
    }
}
