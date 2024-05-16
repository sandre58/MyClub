// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using MyClub.CrossCutting.Localization;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Infrastructure.Packaging;
using MyClub.Teamup.Wpf.Configuration;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Edition;
using MyNet.UI;
using MyNet.UI.Dialogs;
using MyNet.UI.Dialogs.Settings;
using MyNet.UI.Extensions;
using MyNet.UI.Locators;
using MyNet.UI.Messages;
using MyNet.UI.Resources;
using MyNet.UI.Services;
using MyNet.UI.Toasting;
using MyNet.UI.ViewModels;
using MyNet.Utilities;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.IO.AutoSave;
using MyNet.Utilities.IO.FileExtensions;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Messaging;
using MyNet.Utilities.Progress;
using MyNet.Wpf.Extensions;

namespace MyClub.Teamup.Wpf.Services
{
    internal class ProjectCommandsService(ProjectService projectService, ProjectInfoProvider projectInfoProvider, RecentFilesManager recentFilesManager, IAutoSaveService autoSaveService, TeamupConfiguration teamupConfiguration)
    {
        private readonly IAutoSaveService _autoSaveService = autoSaveService;
        private readonly ProjectService _projectService = projectService;
        private readonly ProjectInfoProvider _projectInfoProvider = projectInfoProvider;
        private readonly RecentFilesManager _recentFilesManager = recentFilesManager;
        private readonly TeamupConfiguration _teamupConfiguration = teamupConfiguration;
        private readonly object _lock = new();

        public virtual bool IsEnabled() => !AppBusyManager.MainBusyService.IsBusy && !DialogManager.HasOpenedDialogs;

        public async Task EditAsync()
        {
            if (!EnsureProjectIsLoaded()) return;

            var vm = ViewModelManager.Get<ProjectEditionViewModel>();
            vm.Mode = ScreenMode.Edition;

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
        }

        public async Task EditTeamsAsync()
        {
            if (!EnsureProjectIsLoaded()) return;
            _ = await DialogManager.ShowDialogAsync<MyTeamsEditionViewModel>().ConfigureAwait(false);
        }

        public async Task LoadAsync()
        {
            var (result, filename) = await AskOpenFilenameAsync().ConfigureAwait(false);
            if (result.IsFalse()) return;

            if (!await EnsureProjectIsSavedAsync().ConfigureAwait(false)) return;

            await LoadFileAsync(filename).ConfigureAwait(false);
        }

        public async Task LoadAsync(string filename)
        {
            if (!await EnsureProjectIsSavedAsync().ConfigureAwait(false)) return;

            await LoadFileAsync(filename).ConfigureAwait(false);
        }

        public async Task LoadTemplateAsync(string filename)
        {
            if (!await EnsureProjectIsSavedAsync().ConfigureAwait(false)) return;

            await LoadFileAsync(filename, true).ConfigureAwait(false);
        }

        private async Task LoadFileAsync(string filename, bool isTemplate = false)
        {
            Messenger.Default.Send(new UpdateFileMenuVisibilityRequestedMessage(VisibilityAction.Hide));
            Messenger.Default.Send(new UpdateNotificationsVisibilityRequestedMessage(VisibilityAction.Hide));

            if (!filename.IsTmproj()) throw new TranslatableException(nameof(MyClubResources.FileXNotTmprojError), filename);

            AppBusyManager.Progress();

            try
            {
                using var cancellationTokenSource = new CancellationTokenSource();
                using (_autoSaveService.Suspend())
                using (ProgressManager.NewCancellable([0.6, 0.35, 0.05], cancellationTokenSource.Cancel, nameof(MyClubResources.ProgressLoadingFileX), filename))
                {
                    var project = isTemplate
                        ? await _projectService.LoadTemplateAsync(filename, cancellationTokenSource.Token).ConfigureAwait(false)
                        : await _projectService.LoadAsync(filename, cancellationTokenSource.Token).ConfigureAwait(false);

                    if (project is not null)
                    {
                        AddCurrentFileInRecentFiles();

                        var name = isTemplate ? project.Name : _projectInfoProvider.GetFilename();
                        ToasterManager.ShowSuccess(MyClubResources.ProjectXLoadedSuccess.FormatWith(name));
                        LogManager.Info($"Tmproj file loaded : {filename}");
                    }
                }
            }
            catch (TranslatableException e)
            {
                e.ShowInToaster(true, false);
            }
            catch (OperationCanceledException e)
            {
                LogManager.Warning(e.Message);
                ToasterManager.ShowWarning(MyClubResources.LoadingFileCancelledWarning);
            }
            catch (Exception e)
            {
                LogManager.Error(e);
                new Exception(MyClubResources.ProjectXLoadedError.FormatWith(filename)).ShowInToaster(true, false);
            }
            finally
            {
                AppBusyManager.Resume();
            }
        }

        public async Task NewAsync()
        {
            if (!await EnsureProjectIsSavedAsync().ConfigureAwait(false)) return;

            Messenger.Default.Send(new UpdateFileMenuVisibilityRequestedMessage(VisibilityAction.Hide));
            Messenger.Default.Send(new UpdateNotificationsVisibilityRequestedMessage(VisibilityAction.Hide));

            AppBusyManager.Progress();

            try
            {
                // Force to new thread
                await Task.Delay(10).ConfigureAwait(false);

                using var cancellationTokenSource = new CancellationTokenSource();
                using (_autoSaveService.Suspend())
                using (ProgressManager.NewCancellable(cancellationTokenSource.Cancel, MyClubResources.ProgressNewProject))
                {
                    var project = await _projectService.NewAsync(_teamupConfiguration.Mock.RandomizeData, cancellationTokenSource.Token).ConfigureAwait(false);

                    if (project is not null)
                        LogManager.Info($"New project created : {project.Name} ({project.Club.Teams.Count} team(s), {project.Players.Count} player(s), {project.TrainingSessions.Count} training session(s))");
                }
            }
            catch (OperationCanceledException e)
            {
                LogManager.Warning(e.Message);
                ToasterManager.ShowWarning(MyClubResources.LoadingNewProjectCancelledWarning);
            }
            catch (Exception e)
            {
                LogManager.Error(e);
                e.ShowInToaster(true, false);
            }
            finally
            {
                AppBusyManager.Resume();
            }
        }

        public async Task CreateAsync()
        {
            if (!await EnsureProjectIsSavedAsync().ConfigureAwait(false)) return;

            var vm = ViewModelManager.Get<ProjectEditionViewModel>();
            vm.Mode = ScreenMode.Creation;

            MyNet.Observable.Threading.Scheduler.GetUIOrCurrent().Schedule(async _ =>
            {
                if ((await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false)).IsTrue())
                {
                    await AppBusyManager.ProgressAsync(() =>
                    {
                        using (_autoSaveService.Suspend())
                        using (ProgressManager.New(MyClubResources.ProgressNewProject))
                        {
                            var project = _projectService.New(new ProjectMetadataDto
                            {
                                Name = vm.Name,
                                MainTeamId = vm.MainTeamId,
                                Club = new ClubDto
                                {
                                    Name = vm.ClubName,
                                    HomeColor = vm.HomeColor?.ToHex() ?? string.Empty,
                                    AwayColor = vm.AwayColor?.ToHex() ?? string.Empty,
                                    Country = vm.Country,
                                    Stadium = vm.StadiumSelection.SelectedItem is not null ? new StadiumDto
                                    {
                                        Id = vm.StadiumSelection.SelectedItem.Id,
                                        Name = vm.StadiumSelection.SelectedItem.Name,
                                        Ground = vm.StadiumSelection.SelectedItem.Ground,
                                        Address = vm.StadiumSelection.SelectedItem?.Address,
                                    } : null,
                                },
                                Season = new SeasonDto
                                {
                                    StartDate = vm.StartDate ?? DateTime.Today.BeginningOfYear(),
                                    EndDate = vm.EndDate ?? DateTime.Today.EndOfYear(),
                                },
                                Image = vm.Image,
                                TrainingDuration = vm.TrainingDuration,
                                TrainingStartTime = vm.TrainingStartTime,
                                Category = vm.Category,
                                Color = vm.Color?.ToHex() ?? string.Empty
                            }, _teamupConfiguration.Mock.RandomizeData);
                            LogManager.Info($"New project created : {project.Name} ({project.Club.Teams.Count} team(s), {project.Players.Count} player(s), {project.TrainingSessions.Count} training session(s))");
                        }
                    }).ConfigureAwait(false);
                }
            });
        }

        public async Task<bool> SaveAsync()
        {
            if (!EnsureProjectIsLoaded()) return false;

            var filename = _projectInfoProvider.FilePath;
            if (string.IsNullOrEmpty(filename) || !File.Exists(filename))
            {
                bool? result = null;
                string? filenameTemp = null;
                MyNet.Observable.Threading.Scheduler.GetUIOrCurrent().Schedule(_ =>
                {
                    lock (_lock)
                        (result, filenameTemp) = AskSaveFilenameAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                });

                lock (_lock)
                {
                    if (result.IsFalse()) return false;
                    filename = filenameTemp;
                }
            }

            return await SaveAsync(filename!).ConfigureAwait(false);
        }

        public async Task<bool> SaveAsAsync()
        {
            if (!EnsureProjectIsLoaded()) return false;

            var (result, filename) = await AskSaveFilenameAsync().ConfigureAwait(false);
            return !result.IsFalse() && await SaveAsync(filename).ConfigureAwait(false);
        }

        private async Task<bool> SaveAsync(string filename)
        {
            if (!filename.IsTmproj()) throw new TranslatableException(nameof(MyClubResources.FileXNotTmprojError), filename);

            AppBusyManager.Progress();

            try
            {
                using var cancellationTokenSource = new CancellationTokenSource();
                var result = false;

                using (_autoSaveService.Suspend())
                using (ProgressManager.NewCancellable([0.7, 0.1, 0.05], cancellationTokenSource.Cancel, nameof(MyClubResources.ProgressSaveFileX), filename))
                {
                    result = await _projectService.SaveAsync(filename, cancellationTokenSource.Token).ConfigureAwait(false);
                    AddCurrentFileInRecentFiles();
                }

                ToasterManager.ShowSuccess(MyClubResources.ProjectXSavedSuccess.FormatWith(_projectInfoProvider.Name));
                LogManager.Info($"Project saved in tmproj file : {filename}");

                return result;
            }
            catch (TranslatableException e)
            {
                e.ShowInToaster(true, false);
                return false;
            }
            catch (OperationCanceledException e)
            {
                LogManager.Warning(e.Message);
                ToasterManager.ShowWarning(MyClubResources.SavingCancelledWarning);
                return false;
            }
            catch (Exception e)
            {
                LogManager.Error(e);
                new Exception(MyClubResources.ProjectXSavedError.FormatWith(filename)).ShowInToaster(true, false);
                return false;
            }
            finally
            {
                AppBusyManager.Resume();
            }
        }

        public async Task CloseCurrentProjectAsync()
        {
            if (!await EnsureProjectIsSavedAsync().ConfigureAwait(false)) return;

            await AppBusyManager.WaitAsync(() =>
            {
                using (_autoSaveService.Suspend())
                    _projectService.Close();
            }).ConfigureAwait(false);
        }

        private async Task<(bool? result, string filename)> AskSaveFilenameAsync()
        {
            if (_projectInfoProvider.GetCurrentProject() is null) return (false, string.Empty);

            var builder = new FileExtensionFilterBuilder();
            _ = builder.Add(TmprojFileExtensionInfo.Tmproj);

            var name = !string.IsNullOrEmpty(_projectInfoProvider.FilePath) ? _projectInfoProvider.GetFilename(false)! : _projectInfoProvider.Name?.ToFilename()!;

            var defaultExt = string.IsNullOrEmpty(_projectInfoProvider.FilePath) ?
                            TmprojFileExtensionInfo.TmprojExtension :
                             Path.GetExtension(_projectInfoProvider.FilePath).ToLowerInvariant();

            var settings = new SaveFileDialogSettings
            {
                CheckFileExists = false,
                OverwritePrompt = true,
                FileName = name,
                DefaultExtension = defaultExt,
                Filters = builder.GenerateFilters(),
            };

            return await DialogManager.ShowSaveFileDialogAsync(settings).ConfigureAwait(false);
        }

        private static async Task<(bool? result, string filename)> AskOpenFilenameAsync()
        {
            var builder = new FileExtensionFilterBuilder();
            _ = builder.Add(TmprojFileExtensionInfo.Tmproj);

            var settings = new OpenFileDialogSettings
            {
                CheckFileExists = true,
                Filters = builder.GenerateFilters()
            };

            return await DialogManager.ShowOpenFileDialogAsync(settings).ConfigureAwait(false);
        }

        private bool EnsureProjectIsLoaded()
        {
            if (_projectInfoProvider.GetCurrentProject() is not null) return true;

            ToasterManager.ShowError(MyClubResources.NoProjectLoaded);
            return false;
        }

        public async Task<bool> EnsureProjectIsSavedAsync()
        {
            if (_projectInfoProvider.GetCurrentProject() is null || !_projectInfoProvider.IsDirty) return true;

            var result = await DialogManager.ShowQuestionWithCancelAsync(MyClubResources.ProjectIsDirtyQuestion, UiResources.Edition).ConfigureAwait(false);
            return result == MessageBoxResult.Yes ? await SaveAsync().ConfigureAwait(false) : result == MessageBoxResult.No;
        }

        private void AddCurrentFileInRecentFiles()
        {
            using (ProgressManager.StartUncancellable(MyClubResources.ProgressAddRecentFile))
                if (_projectInfoProvider.GetCurrentProject() is not null && !string.IsNullOrEmpty(_projectInfoProvider.FilePath))
                    _recentFilesManager.Add(_projectInfoProvider.Name!, _projectInfoProvider.FilePath);
        }
    }
}
