// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Edition;
using MyClub.UserContext.Domain.UserAggregate;
using MyNet.UI;
using MyNet.UI.Busy;
using MyNet.UI.Commands;
using MyNet.UI.Dialogs;
using MyNet.UI.Messages;
using MyNet.UI.Navigation;
using MyNet.UI.Notifications;
using MyNet.UI.Services;
using MyNet.UI.ViewModels.FileHistory;
using MyNet.UI.ViewModels.Shell;
using MyNet.Utilities;
using MyNet.Utilities.IO.AutoSave;
using MyNet.Utilities.Messaging;
using MyNet.Wpf.DragAndDrop;

namespace MyClub.Teamup.Wpf.ViewModels.Shell
{
    internal class MainWindowViewModel : MainWindowViewModelBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ProjectInfoProvider _projectInfoProvider;
        private readonly ProjectCommandsService _projectCommandsService;
        private readonly CompositeDisposable _disposables = [];

        public int CountTeams { get; private set; }

        public int CountTactics { get; private set; }

        public int CountPlayers { get; private set; }

        public int CountTrainingSessions { get; private set; }

        public int CountInjuries { get; private set; }

        public int CountCompetitions { get; private set; }

        public User User => _userRepository.GetCurrent();

        public INavigationService NavigationService { get; }

        public IDialogService DialogService { get; }

        public IBusyService BackgroundBusyService { get; }

        public FileDropHandler DropHandler { get; }

        public SearchViewModel SearchViewModel { get; }

        public virtual bool CanDropTmprojFiles => NavigationService.CurrentContext?.Page is PageViewModel page && page.CanDropTmprojFiles;

        public ICommand GoBackCommand { get; }

        public ICommand GoForwardCommand { get; }

        public ICommand ToggleOpenCommand { get; }

        public ICommand TogglePropertiesCommand { get; }

        public ICommand ToggleAboutCommand { get; }

        public ICommand TogglePreferencesCommand { get; }

        public ICommand OpenUserProfileCommand { get; }

        public ICommand OpenSettingsCommand { get; }

        public ICommand LoadCommand { get; }

        public ICommand NewCommand { get; }

        public ICommand CreateCommand { get; }

        public ICommand CloseCommand { get; }

        public ICommand SaveCommand { get; }

        public ICommand SaveAsCommand { get; }

        public ICommand EditProjectCommand { get; }

        public ICommand EditTeamsCommand { get; }

        public bool IsDirty => _projectInfoProvider.IsDirty;

        public string? Filename => _projectInfoProvider.GetFilename();

        public override string Title
        {
            get
            {
                var str = new StringBuilder();

                if (!string.IsNullOrEmpty(_projectInfoProvider.Name))
                {
                    _ = str.Append(_projectInfoProvider.Name);
                    _ = str.Append(" - ");
                }
                _ = str.Append(ProductName);

                return str.ToString();
            }
        }

        public Project? CurrentProject { get; private set; }

        public bool HasCurrentProject { get; private set; }

        public bool ShowNavigationControls => HasCurrentProject && !FileMenuViewModel.IsVisible;

        public MainWindowViewModel(
            INavigationService navigationService,
            IDialogService dialogService,
            INotificationsManager notificationManager,
            IUserRepository userRepository,
            IAppCommandsService appCommandsService,
            IPersistentPreferencesService persistentPreferencesService,
            IAutoSaveService autoSaveService,
            ProjectCommandsService projectCommandsService,
            ProjectInfoProvider projectInfoProvider,
            TeamsProvider teamsProvider,
            MainItemsProvider mainItemsProvider,
            TacticsProvider tacticsProvider,
            NavigableItemsProvider navigationItemsProvider,
            RecentFilesViewModel recentFilesViewModel
            )
            : base(new FileMenuViewModel(projectInfoProvider, projectCommandsService, appCommandsService, persistentPreferencesService, autoSaveService, recentFilesViewModel),
                  notificationManager,
                  appCommandsService,
                  AppBusyManager.MainBusyService)
        {
            _userRepository = userRepository;
            _projectInfoProvider = projectInfoProvider;
            _projectCommandsService = projectCommandsService;
            DropHandler = new(async (_, files) => await OnDropFilesAsync(files).ConfigureAwait(false));
            NavigationService = navigationService;
            BackgroundBusyService = AppBusyManager.BackgroundBusyService;
            DialogService = dialogService;
            SearchViewModel = new(navigationItemsProvider);

            GoBackCommand = CommandsManager.Create(() => NavigationService.GoBack(), () => NavigationService.CanGoBack() && !DialogManager.HasOpenedDialogs);
            GoForwardCommand = CommandsManager.Create(() => NavigationService.GoForward(), () => NavigationService.CanGoForward() && !DialogManager.HasOpenedDialogs);
            ToggleOpenCommand = CommandsManager.Create(() => Messenger.Default.Send(new UpdateFileMenuContentVisibilityRequestedMessage(typeof(OpenViewModel), VisibilityAction.Toggle)), () => !DialogManager.HasOpenedDialogs);
            TogglePropertiesCommand = CommandsManager.Create(() => Messenger.Default.Send(new UpdateFileMenuContentVisibilityRequestedMessage(typeof(PropertiesViewModel), VisibilityAction.Toggle)), () => HasCurrentProject && !DialogManager.HasOpenedDialogs);
            ToggleAboutCommand = CommandsManager.Create(() => Messenger.Default.Send(new UpdateFileMenuContentVisibilityRequestedMessage(typeof(AboutViewModel), VisibilityAction.Toggle)), () => !DialogManager.HasOpenedDialogs);
            TogglePreferencesCommand = CommandsManager.Create(() => Messenger.Default.Send(new UpdateFileMenuContentVisibilityRequestedMessage(typeof(PreferencesViewModel), VisibilityAction.Toggle)), () => !DialogManager.HasOpenedDialogs);
            OpenUserProfileCommand = CommandsManager.Create(async () => await DialogManager.ShowDialogAsync<UserEditionViewModel>(), () => !DialogManager.HasOpenedDialogs);
            OpenSettingsCommand = CommandsManager.Create(async () => await DialogManager.ShowDialogAsync<SettingsEditionViewModel>().ConfigureAwait(false), () => !DialogManager.HasOpenedDialogs);
            LoadCommand = CommandsManager.Create(async () => await projectCommandsService.LoadAsync().ConfigureAwait(false), projectCommandsService.IsEnabled);
            NewCommand = CommandsManager.Create(async () => await projectCommandsService.NewAsync().ConfigureAwait(false), projectCommandsService.IsEnabled);
            CreateCommand = CommandsManager.Create(async () => await projectCommandsService.CreateAsync().ConfigureAwait(false), projectCommandsService.IsEnabled);
            CloseCommand = CommandsManager.Create(async () => await projectCommandsService.CloseCurrentProjectAsync().ConfigureAwait(false), () => HasCurrentProject && projectCommandsService.IsEnabled());
            SaveCommand = CommandsManager.Create(async () => await projectCommandsService.SaveAsync().ConfigureAwait(false), () => HasCurrentProject && projectCommandsService.IsEnabled());
            SaveAsCommand = CommandsManager.Create(async () => await projectCommandsService.SaveAsAsync().ConfigureAwait(false), () => HasCurrentProject && projectCommandsService.IsEnabled());
            EditProjectCommand = CommandsManager.Create(async () => await projectCommandsService.EditAsync().ConfigureAwait(false), () => HasCurrentProject && projectCommandsService.IsEnabled());
            EditTeamsCommand = CommandsManager.Create(async () => await projectCommandsService.EditTeamsAsync().ConfigureAwait(false), () => HasCurrentProject && projectCommandsService.IsEnabled());

            _disposables.AddRange(
            [
                FileMenuViewModel.WhenPropertyChanged(x => x.IsVisible).Subscribe(_ => RaisePropertyChanged(nameof(ShowNavigationControls))),
                _projectInfoProvider.WhenAnyPropertyChanged(nameof(ProjectInfoProvider.Name), nameof(ProjectInfoProvider.FilePath), nameof(ProjectInfoProvider.IsDirty)).Subscribe(_ => RefreshTitle()),
                tacticsProvider.Connect().Subscribe(_ => CountTactics = tacticsProvider.Count),
                teamsProvider.ConnectMyTeams().Subscribe(_ => CountTeams = teamsProvider.Items.Count(x => x.IsMyTeam)),
                mainItemsProvider.ConnectPlayers().AutoRefreshOnObservable(x => x.Injuries.ToObservableChangeSet().WhenAnyPropertyChanged()).Subscribe(_ =>
                {
                    CountPlayers = mainItemsProvider.Players.Count;
                    CountInjuries = mainItemsProvider.Players.Sum(x => x.Injuries.Count(y => y.IsCurrent));
                }),
                mainItemsProvider.ConnectTrainingSessions().Subscribe(_ => CountTrainingSessions = mainItemsProvider.TrainingSessions.Count),
                mainItemsProvider.ConnectCompetitions().Subscribe(_ => CountCompetitions = mainItemsProvider.Competitions.Count)
            ]);

            NavigationService.Navigated += OnNavigatedCallback;
            _projectInfoProvider.WhenProjectChanged(x =>
            {
                CurrentProject = x;
                HasCurrentProject = x is not null;

                CloseDrawers();

                RefreshTitle();

                RaisePropertyChanged(nameof(ShowNavigationControls));
            });
        }

        private void OnNavigatedCallback(object? sender, NavigationEventArgs _) => RaisePropertyChanged(nameof(CanDropTmprojFiles));

        private async Task OnDropFilesAsync(StringCollection files)
        {
            if (files.Count == 0) return;

            var file = files[0];

            if (!string.IsNullOrEmpty(file) && File.Exists(file))
                await _projectCommandsService.LoadAsync(file).ConfigureAwait(false);
        }

        #region Refresh

        private void RefreshTitle()
        {
            RaisePropertyChanged(nameof(Title));
            RaisePropertyChanged(nameof(IsDirty));
            RaisePropertyChanged(nameof(Filename));
        }

        #endregion

        protected override void Cleanup()
        {
            NavigationService.Navigated -= OnNavigatedCallback;
            Messenger.Default.Unregister(this);
            NotificationsViewModel.Dispose();
            _disposables.Dispose();
            base.Cleanup();
        }
    }
}
