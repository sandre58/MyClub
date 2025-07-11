// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData.Binding;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyNet.UI.Commands;
using MyNet.UI.Services;
using MyNet.UI.ViewModels.FileHistory;
using MyNet.UI.ViewModels.Shell;
using MyNet.Utilities.IO.AutoSave;
using MyNet.Utilities.Messaging;

namespace MyClub.Scorer.Wpf.ViewModels.Shell
{
    internal class FileMenuViewModel : FileMenuViewModelBase
    {
        public bool ProjectIsLoaded { get; private set; }

        public bool PreferencesIsVisible => ContentIsVisible<PreferencesViewModel>();

        public bool OpenIsVisible => ContentIsVisible<OpenViewModel>();

        public bool AboutIsVisible => ContentIsVisible<AboutViewModel>();

        public bool PropertiesIsVisible => ContentIsVisible<PropertiesViewModel>();

        public ICommand NewLeagueCommand { get; }

        public ICommand NewCupCommand { get; }

        public ICommand CreateCommand { get; }

        public ICommand CloseCommand { get; }

        public ICommand SaveCommand { get; }

        public ICommand SaveAsCommand { get; }

        public FileMenuViewModel(
            ProjectInfoProvider projectInfoProvider,
            ProjectCommandsService projectCommandsService,
            IAppCommandsService appCommandsService,
            IPersistentPreferencesService persistentPreferencesService,
            IAutoSaveService autoSaveService,
            RecentFilesViewModel recentFilesViewModel
            )
            : base(
                   [
                        new AboutViewModel(),
                        new PreferencesViewModel(persistentPreferencesService, autoSaveService),
                        new OpenViewModel(recentFilesViewModel, projectCommandsService),
                        new PropertiesViewModel(projectInfoProvider)
                   ], appCommandsService)
        {
            NewLeagueCommand = CommandsManager.Create(async () => await projectCommandsService.NewAsync(CompetitionType.League).ConfigureAwait(false), projectCommandsService.IsEnabled);
            NewCupCommand = CommandsManager.Create(async () => await projectCommandsService.NewAsync(CompetitionType.Cup).ConfigureAwait(false), projectCommandsService.IsEnabled);
            CreateCommand = CommandsManager.Create(async () => await projectCommandsService.CreateAsync().ConfigureAwait(false), projectCommandsService.IsEnabled);
            CloseCommand = CommandsManager.Create(async () => await projectCommandsService.CloseCurrentProjectAsync().ConfigureAwait(false), () => ProjectIsLoaded && projectCommandsService.IsEnabled());
            SaveCommand = CommandsManager.Create(async () => await projectCommandsService.SaveAsync().ConfigureAwait(false), () => ProjectIsLoaded && projectCommandsService.IsEnabled());
            SaveAsCommand = CommandsManager.Create(async () => await projectCommandsService.SaveAsAsync().ConfigureAwait(false), () => ProjectIsLoaded && projectCommandsService.IsEnabled());

            Disposables.Add(projectInfoProvider.WhenPropertyChanged(x => x.IsLoaded).Subscribe(x => ProjectIsLoaded = x.Value));
        }

        protected override void OnContentChanged()
        {
            RaisePropertyChanged(nameof(PreferencesIsVisible));
            RaisePropertyChanged(nameof(OpenIsVisible));
            RaisePropertyChanged(nameof(AboutIsVisible));
            RaisePropertyChanged(nameof(PropertiesIsVisible));
        }

        protected override void Cleanup()
        {
            Messenger.Default.Unregister(this);
            base.Cleanup();
        }
    }
}
