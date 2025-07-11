// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Specialized;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Edition;
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

namespace MyClub.Scorer.Wpf.ViewModels.Shell
{
    internal class MainWindowViewModel : MainWindowViewModelBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ProjectCommandsService _projectCommandsService;

        public MainWindowViewModel(INavigationService navigationService,
                                   IDialogService dialogService,
                                   INotificationsManager notificationManager,
                                   IUserRepository userRepository,
                                   IAppCommandsService appCommandsService,
                                   IPersistentPreferencesService persistentPreferencesService,
                                   IAutoSaveService autoSaveService,
                                   ProjectCommandsService projectCommandsService,
                                   CompetitionCommandsService competitionCommandsService,
                                   ProjectInfoProvider projectInfoProvider,
                                   CompetitionInfoProvider competitionInfoProvider,
                                   CompetitionStagesProvider competitionStagesProvider,
                                   MatchesProvider matchesProvider,
                                   TeamsProvider teamsProvider,
                                   StadiumsProvider stadiumsProvider,
                                   RecentFilesViewModel recentFilesViewModel)
    : base(new FileMenuViewModel(projectInfoProvider, projectCommandsService, appCommandsService, persistentPreferencesService, autoSaveService, recentFilesViewModel),
           notificationManager,
           appCommandsService,
           AppBusyManager.MainBusyService)
        {
            _userRepository = userRepository;
            _projectCommandsService = projectCommandsService;
            DropHandler = new(async (_, files) => await OnDropFilesAsync(files).ConfigureAwait(false));
            NavigationService = navigationService;
            BackgroundBusyService = AppBusyManager.BackgroundBusyService;
            DialogService = dialogService;

            GoBackCommand = CommandsManager.Create(() => NavigationService.GoBack(), () => NavigationService.CanGoBack() && !DialogManager.HasOpenedDialogs);
            GoForwardCommand = CommandsManager.Create(() => NavigationService.GoForward(), () => NavigationService.CanGoForward() && !DialogManager.HasOpenedDialogs);
            ToggleOpenCommand = CommandsManager.Create(() => Messenger.Default.Send(new UpdateFileMenuContentVisibilityRequestedMessage(typeof(OpenViewModel), VisibilityAction.Toggle)), () => !DialogManager.HasOpenedDialogs);
            TogglePropertiesCommand = CommandsManager.Create(() => Messenger.Default.Send(new UpdateFileMenuContentVisibilityRequestedMessage(typeof(PropertiesViewModel), VisibilityAction.Toggle)), () => ProjectIsLoaded && !DialogManager.HasOpenedDialogs);
            ToggleAboutCommand = CommandsManager.Create(() => Messenger.Default.Send(new UpdateFileMenuContentVisibilityRequestedMessage(typeof(AboutViewModel), VisibilityAction.Toggle)), () => !DialogManager.HasOpenedDialogs);
            TogglePreferencesCommand = CommandsManager.Create(() => Messenger.Default.Send(new UpdateFileMenuContentVisibilityRequestedMessage(typeof(PreferencesViewModel), VisibilityAction.Toggle)), () => !DialogManager.HasOpenedDialogs);
            OpenUserProfileCommand = CommandsManager.Create(async () => await DialogManager.ShowDialogAsync<UserEditionViewModel>(), () => !DialogManager.HasOpenedDialogs);
            OpenSettingsCommand = CommandsManager.Create(async () => await DialogManager.ShowDialogAsync<SettingsEditionViewModel>().ConfigureAwait(false), () => !DialogManager.HasOpenedDialogs);
            LoadCommand = CommandsManager.Create(async () => await projectCommandsService.LoadAsync().ConfigureAwait(false), projectCommandsService.IsEnabled);
            NewLeagueCommand = CommandsManager.Create(async () => await projectCommandsService.NewAsync(CompetitionType.League).ConfigureAwait(false), projectCommandsService.IsEnabled);
            NewCupCommand = CommandsManager.Create(async () => await projectCommandsService.NewAsync(CompetitionType.Cup).ConfigureAwait(false), projectCommandsService.IsEnabled);
            CreateCommand = CommandsManager.Create(async () => await projectCommandsService.CreateAsync().ConfigureAwait(false), projectCommandsService.IsEnabled);
            CloseCommand = CommandsManager.Create(async () => await projectCommandsService.CloseCurrentProjectAsync().ConfigureAwait(false), () => ProjectIsLoaded && projectCommandsService.IsEnabled());
            SaveCommand = CommandsManager.Create(async () => await projectCommandsService.SaveAsync().ConfigureAwait(false), () => ProjectIsLoaded && projectCommandsService.IsEnabled());
            SaveAsCommand = CommandsManager.Create(async () => await projectCommandsService.SaveAsAsync().ConfigureAwait(false), () => ProjectIsLoaded && projectCommandsService.IsEnabled());
            OpenBuildAssistantCommand = CommandsManager.Create(async () => await competitionCommandsService.OpenBuildAssistantAsync().ConfigureAwait(false), () => ProjectIsLoaded && projectCommandsService.IsEnabled());
            EditRankingRulesCommand = CommandsManager.Create(async () => await competitionCommandsService.EditRankingRulesAsync().ConfigureAwait(false), () => IsLeague && projectCommandsService.IsEnabled());
            EditProjectCommand = CommandsManager.Create(async () => await projectCommandsService.EditAsync().ConfigureAwait(false), () => ProjectIsLoaded && projectCommandsService.IsEnabled());

            Disposables.AddRange(
            [
                FileMenuViewModel.WhenPropertyChanged(x => x.IsVisible).Subscribe(_ => RaisePropertyChanged(nameof(ShowNavigationControls))),
                projectInfoProvider.WhenPropertyChanged(x => x.Name).Subscribe(x =>
                {
                    Name = x.Value;
                    RefreshTitle();
                }),
                projectInfoProvider.WhenPropertyChanged(x => x.Image).Subscribe(x => Image = x.Value),
                projectInfoProvider.WhenPropertyChanged(x => x.IsDirty).Subscribe(x => IsDirty = x.Value),
                projectInfoProvider.WhenPropertyChanged(x => x.FilePath).Subscribe(_ => Filename = projectInfoProvider.GetFilename()),
                projectInfoProvider.WhenPropertyChanged(x => x.IsLoaded).Subscribe(x =>
                {
                    ProjectIsLoaded = x.Value;

                    CloseDrawers();

                    Messenger.Default.Send(new UpdateFileMenuContentVisibilityRequestedMessage(typeof(PropertiesViewModel), VisibilityAction.Hide));
                    RaisePropertyChanged(nameof(ShowNavigationControls));
                }),
                competitionInfoProvider.WhenPropertyChanged(x => x.Type).Subscribe(x =>
                {
                    IsLeague = x.Value is CompetitionType.League;
                    IsCup = x.Value is CompetitionType.Cup;
                }),
                matchesProvider.Connect().Subscribe(_ => CountMatches = matchesProvider.Count),
                competitionStagesProvider.Connect().Subscribe(_ => CountCompetitionStages = competitionStagesProvider.Count),
                teamsProvider.Connect().Subscribe(_ => CountTeams = teamsProvider.Count),
                stadiumsProvider.Connect().Subscribe(_ => CountStadiums = stadiumsProvider.Count),
            ]);

            NavigationService.Navigated += OnNavigatedCallback;
        }

        public int CountCompetitionStages { get; private set; }

        public int CountMatches { get; private set; }

        public int CountTeams { get; private set; }

        public int CountStadiums { get; private set; }

        public User User => _userRepository.GetCurrent();

        public INavigationService NavigationService { get; }

        public IDialogService DialogService { get; }

        public IBusyService BackgroundBusyService { get; }

        public FileDropHandler DropHandler { get; }

        public virtual bool CanDropScprojFiles => NavigationService.CurrentContext?.Page is PageViewModel page && page.CanDropScprojFiles;

        public ICommand GoBackCommand { get; }

        public ICommand GoForwardCommand { get; }

        public ICommand ToggleOpenCommand { get; }

        public ICommand TogglePropertiesCommand { get; }

        public ICommand ToggleAboutCommand { get; }

        public ICommand TogglePreferencesCommand { get; }

        public ICommand OpenUserProfileCommand { get; }

        public ICommand OpenSettingsCommand { get; }

        public ICommand LoadCommand { get; }

        public ICommand NewLeagueCommand { get; }

        public ICommand NewCupCommand { get; }

        public ICommand CreateCommand { get; }

        public ICommand EditProjectCommand { get; }

        public ICommand EditRankingRulesCommand { get; }

        public ICommand CloseCommand { get; }

        public ICommand SaveCommand { get; }

        public ICommand SaveAsCommand { get; }

        public ICommand OpenBuildAssistantCommand { get; }

        public bool IsDirty { get; private set; }

        public string? Filename { get; private set; }

        public string? Name { get; private set; }

        public byte[]? Image { get; private set; }

        public override string Title
        {
            get
            {
                var str = new StringBuilder();

                if (!string.IsNullOrEmpty(Name))
                {
                    _ = str.Append(Name);
                    _ = str.Append(" - ");
                }
                _ = str.Append(ProductName);

                return str.ToString();
            }
        }

        public bool IsLeague { get; private set; }

        public bool IsCup { get; private set; }

        public bool ProjectIsLoaded { get; private set; }

        public bool ShowNavigationControls => ProjectIsLoaded && !FileMenuViewModel.IsVisible;

        private void OnNavigatedCallback(object? sender, NavigationEventArgs _) => RaisePropertyChanged(nameof(CanDropScprojFiles));

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
            base.Cleanup();
        }
    }
}
