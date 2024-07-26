// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Messages;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Infrastructure.Packaging;
using MyClub.Scorer.Wpf.Messages;
using MyClub.Scorer.Wpf.Services.Deferrers;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Edition;
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
using MyNet.Utilities.Extensions;
using MyNet.Utilities.IO.AutoSave;
using MyNet.Utilities.IO.FileExtensions;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Messaging;
using MyNet.Utilities.Progress;

namespace MyClub.Scorer.Wpf.Services
{
    internal class ProjectCommandsService(ProjectService projectService,
                                          ProjectInfoProvider projectInfoProvider,
                                          RecentFilesManager recentFilesManager,
                                          IAutoSaveService autoSaveService)
    {
        private readonly IAutoSaveService _autoSaveService = autoSaveService;
        private readonly ProjectService _projectService = projectService;
        private readonly ProjectInfoProvider _projectInfoProvider = projectInfoProvider;
        private readonly RecentFilesManager _recentFilesManager = recentFilesManager;
        private readonly object _lock = new();

        public virtual bool IsEnabled() => !AppBusyManager.MainBusyService.IsBusy && !DialogManager.HasOpenedDialogs;

        public async Task EditAsync()
        {
            if (!EnsureProjectIsLoaded()) return;

            var vm = ViewModelManager.Get<ProjectEditionViewModel>();
            vm.Mode = ScreenMode.Edition;

            _ = await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false);
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

            if (!filename.IsScproj()) throw new TranslatableException(nameof(MyClubResources.FileXNotScprojError), filename);

            AppBusyManager.Progress();

            try
            {
                OnProjectLoading();

                using var cancellationTokenSource = new CancellationTokenSource();
                using (_autoSaveService.Suspend())
                using (ProgressManager.NewCancellable([0.6, 0.35, 0.05], cancellationTokenSource.Cancel, nameof(MyClubResources.ProgressLoadingFileX), filename))
                {
                    var project = await _projectService.LoadAsync(filename, cancellationTokenSource.Token).ConfigureAwait(false);

                    if (project is not null)
                    {
                        OnProjectLoaded(project, isTemplate ? null : filename);

                        var name = isTemplate ? project.Name : _projectInfoProvider.GetFilename();
                        ToasterManager.ShowSuccess(MyClubResources.ProjectXLoadedSuccess.FormatWith(name));
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

        public async Task NewAsync(CompetitionType type)
        {
            if (!await EnsureProjectIsSavedAsync().ConfigureAwait(false)) return;

            Messenger.Default.Send(new UpdateFileMenuVisibilityRequestedMessage(VisibilityAction.Hide));
            Messenger.Default.Send(new UpdateNotificationsVisibilityRequestedMessage(VisibilityAction.Hide));

            AppBusyManager.Progress();

            try
            {
                OnProjectLoading();

                // Force to new thread
                await Task.Delay(10).ConfigureAwait(false);

                using var cancellationTokenSource = new CancellationTokenSource();
                using (_autoSaveService.Suspend())
                using (ProgressManager.NewCancellable(cancellationTokenSource.Cancel, MyClubResources.ProgressNewProject))
                {
                    var project = await _projectService.NewAsync(type, cancellationTokenSource.Token).ConfigureAwait(false);

                    if (project is not null)
                        OnProjectLoaded(project);
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

            MyNet.UI.Threading.Scheduler.GetUIOrCurrent().Schedule(async _ =>
            {
                if ((await DialogManager.ShowDialogAsync(vm).ConfigureAwait(false)).IsTrue())
                {
                    await AppBusyManager.ProgressAsync(() =>
                    {
                        OnProjectLoading();

                        using (_autoSaveService.Suspend())
                        using (ProgressManager.New(MyClubResources.ProgressNewProject))
                        {
                            var project = Create(CompetitionType.League, new ProjectMetadataDto
                            {
                                Name = vm.Name,
                                Image = vm.Image
                            });

                            if (project is not null)
                                OnProjectLoaded(project);
                        }
                    }).ConfigureAwait(false);
                }
            });
        }

        public async Task CreateAsync(CompetitionType type, ProjectMetadataDto metadata)
        {
            if (!await EnsureProjectIsSavedAsync().ConfigureAwait(false)) return;

            // Force to new thread
            await Task.Delay(10).ConfigureAwait(false);

            await AppBusyManager.ProgressAsync(() =>
            {
                OnProjectLoading();

                using (_autoSaveService.Suspend())
                using (ProgressManager.New(MyClubResources.ProgressNewProject))
                {
                    var project = Create(type, metadata);

                    if (project is not null)
                        OnProjectLoaded(project);
                }
            }).ConfigureAwait(false);
        }

        private IProject Create(CompetitionType type, ProjectMetadataDto metadata)
            => type switch
            {
                CompetitionType.League => _projectService.NewLeague(metadata),
                CompetitionType.Cup => _projectService.NewCup(metadata),
                CompetitionType.Tournament => _projectService.NewTournament(metadata),
                _ => throw new InvalidOperationException(),
            };

        private void OnProjectLoading()
        {
            if (_projectInfoProvider.IsLoaded)
                Messenger.Default.Send(new CurrentProjectCloseRequestMessage());
        }

        private void OnProjectLoaded(IProject project, string? filename = null)
        {
            Messenger.Default.Send(new CurrentProjectLoadedMessage(project, filename));
            LogManager.Info($"Project loaded : {project.Name}{(!string.IsNullOrEmpty(filename) ? $" ({filename})" : string.Empty)}");

            if (!string.IsNullOrEmpty(filename))
                AddCurrentFileInRecentFiles(project.Name, filename);
        }

        public async Task<bool> SaveAsync()
        {
            if (!EnsureProjectIsLoaded()) return false;

            var filename = _projectInfoProvider.FilePath;
            if (string.IsNullOrEmpty(filename) || !File.Exists(filename))
            {
                bool? result = null;
                string? filenameTemp = null;
                MyNet.UI.Threading.Scheduler.GetUIOrCurrent().Schedule(_ =>
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
            if (!filename.IsScproj()) throw new TranslatableException(nameof(MyClubResources.FileXNotScprojError), filename);

            AppBusyManager.Progress();

            try
            {
                using var cancellationTokenSource = new CancellationTokenSource();
                var result = false;

                using (_autoSaveService.Suspend())
                using (ProgressManager.NewCancellable([0.7, 0.1, 0.05], cancellationTokenSource.Cancel, nameof(MyClubResources.ProgressSaveFileX), filename))
                {
                    result = await _projectService.SaveAsync(filename, cancellationTokenSource.Token).ConfigureAwait(false);

                    if (result)
                    {
                        AddCurrentFileInRecentFiles(_projectInfoProvider.Name.OrEmpty(), filename);
                        Messenger.Default.Send(new CurrentProjectSavedMessage(filename));
                    }
                }

                ToasterManager.ShowSuccess(MyClubResources.ProjectXSavedSuccess.FormatWith(_projectInfoProvider.Name));
                LogManager.Info($"Project saved in scproj file : {filename}");

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
                {
                    Messenger.Default.Send(new CurrentProjectCloseRequestMessage());

                    _projectService.Close();
                }
            }).ConfigureAwait(false);
        }

        private async Task<(bool? result, string filename)> AskSaveFilenameAsync()
        {
            if (!_projectInfoProvider.IsLoaded) return (false, string.Empty);

            var builder = new FileExtensionFilterBuilder();
            _ = builder.Add(ScprojFileExtensionInfo.Scproj);

            var name = !string.IsNullOrEmpty(_projectInfoProvider.FilePath) ? _projectInfoProvider.GetFilename(false)! : _projectInfoProvider.Name?.ToFilename()!;

            var defaultExt = string.IsNullOrEmpty(_projectInfoProvider.FilePath) ?
                            ScprojFileExtensionInfo.ScprojExtension :
                             Path.GetExtension(_projectInfoProvider.FilePath).ToLowerInvariant();

            var settings = new SaveFileDialogSettings
            {
                CheckFileExists = false,
                OverwritePrompt = true,
                FileName = name,
                DefaultExtension = defaultExt,
                Filters = builder.GenerateFilters(x => x.Translate()),
            };

            return await DialogManager.ShowSaveFileDialogAsync(settings).ConfigureAwait(false);
        }

        private static async Task<(bool? result, string filename)> AskOpenFilenameAsync()
        {
            var builder = new FileExtensionFilterBuilder();
            _ = builder.Add(ScprojFileExtensionInfo.Scproj);

            var settings = new OpenFileDialogSettings
            {
                CheckFileExists = true,
                Filters = builder.GenerateFilters(x => x.Translate())
            };

            return await DialogManager.ShowOpenFileDialogAsync(settings).ConfigureAwait(false);
        }

        private bool EnsureProjectIsLoaded()
        {
            if (_projectInfoProvider.IsLoaded) return true;

            ToasterManager.ShowError(MyClubResources.NoProjectLoaded);
            return false;
        }

        public async Task<bool> EnsureProjectIsSavedAsync()
        {
            if (!_projectInfoProvider.IsLoaded || !_projectInfoProvider.IsDirty) return true;

            var result = await DialogManager.ShowQuestionWithCancelAsync(MyClubResources.ProjectIsDirtyQuestion, UiResources.Edition).ConfigureAwait(false);
            return result == MessageBoxResult.Yes ? await SaveAsync().ConfigureAwait(false) : result == MessageBoxResult.No;
        }

        private void AddCurrentFileInRecentFiles(string name, string filePath)
        {
            using (ProgressManager.StartUncancellable(MyClubResources.ProgressAddRecentFile))
                if (_projectInfoProvider.IsLoaded && !string.IsNullOrEmpty(filePath))
                    _recentFilesManager.Add(name, filePath);
        }
    }
}
