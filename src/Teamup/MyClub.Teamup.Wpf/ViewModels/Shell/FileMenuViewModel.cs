// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyNet.UI.Commands;
using MyNet.UI.Services;
using MyNet.UI.ViewModels.FileHistory;
using MyNet.UI.ViewModels.Shell;
using MyNet.Utilities.IO.AutoSave;
using MyNet.Utilities.Messaging;

namespace MyClub.Teamup.Wpf.ViewModels.Shell
{
    internal class FileMenuViewModel : FileMenuViewModelBase
    {
        public bool HasCurrentProject { get; private set; }

        public bool PreferencesIsVisible => ContentIsVisible<PreferencesViewModel>();

        public bool OpenIsVisible => ContentIsVisible<OpenViewModel>();

        public bool AboutIsVisible => ContentIsVisible<AboutViewModel>();

        public bool PropertiesIsVisible => ContentIsVisible<PropertiesViewModel>();

        public ICommand NewCommand { get; }

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
            projectInfoProvider.WhenProjectChanged(x => HasCurrentProject = x is not null);

            NewCommand = CommandsManager.Create(async () => await projectCommandsService.NewAsync().ConfigureAwait(false), projectCommandsService.IsEnabled);
            CreateCommand = CommandsManager.Create(async () => await projectCommandsService.CreateAsync().ConfigureAwait(false), projectCommandsService.IsEnabled);
            CloseCommand = CommandsManager.Create(async () => await projectCommandsService.CloseCurrentProjectAsync().ConfigureAwait(false), () => HasCurrentProject && projectCommandsService.IsEnabled());
            SaveCommand = CommandsManager.Create(async () => await projectCommandsService.SaveAsync().ConfigureAwait(false), () => HasCurrentProject && projectCommandsService.IsEnabled());
            SaveAsCommand = CommandsManager.Create(async () => await projectCommandsService.SaveAsAsync().ConfigureAwait(false), () => HasCurrentProject && projectCommandsService.IsEnabled());
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
