// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DynamicData.Binding;
using MyClub.CrossCutting.FileSystem;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Infrastructure.Packaging;
using MyClub.Scorer.Wpf.Services.Providers;
using MyNet.UI.Busy.Models;
using MyNet.UI.Services;
using MyNet.Utilities;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Helpers;
using MyNet.Utilities.IO.AutoSave;
using MyNet.Utilities.IO.FileHistory;
using MyNet.Utilities.Logging;

namespace MyClub.Scorer.Wpf.Services
{
    internal class AutoSaveService : AutoSaveServiceBase
    {
        private readonly ITempService _tempService;
        private readonly ProjectInfoProvider _projectInfoProvider;
        private readonly ProjectService _projectService;
        private readonly IRecentFileRepository _recentFileRepository;
        private string? _lastAutoSaveFilename;

        public AutoSaveService(ProjectInfoProvider projectInfoProvider,
                               ProjectService projectService,
                               ITempService tempService,
                               IRecentFileRepository recentFileRepository,
                               bool isEnabled = true,
                               int intervalInSeconds = 300)
            : base(isEnabled, intervalInSeconds)
        {
            _projectInfoProvider = projectInfoProvider;
            _tempService = tempService;
            _projectService = projectService;
            _recentFileRepository = recentFileRepository;

            _projectInfoProvider.WhenPropertyChanged(x => x.IsDirty).Subscribe(x =>
                x.Value.IfFalse(() =>
                {
                    Stop();
                    Clean();
                }, Start));

            _projectInfoProvider.WhenProjectClosing(() =>
            {
                Stop();
                Clean();
            });

            _projectInfoProvider.WhenProjectLoaded(_ => Start());
        }

        protected override async Task<bool> SaveCoreAsync(CancellationToken? cancellationToken = null)
        {
            if (!_projectInfoProvider.IsLoaded || !_projectInfoProvider.IsDirty) return false;

            AppBusyManager.BackgroundBusyService.Wait<IndeterminateBusy>();

            try
            {
                _lastAutoSaveFilename = _tempService.GetFileName(ScprojFileExtensionInfo.ScprojExtension, _projectInfoProvider.Name);

                using (LogManager.MeasureTime($"Auto Save file : {_lastAutoSaveFilename}", TraceLevel.Debug))
                {
                    var result = await _projectService.SaveAsync(_lastAutoSaveFilename, cancellationToken).ConfigureAwait(false);

                    if (result)
                    {
                        // Add in registry
                        _ = _recentFileRepository.Add(new RecentFile(_projectInfoProvider.Name!, _lastAutoSaveFilename, null, null, false, true));
                    }
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
