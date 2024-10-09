// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MyClub.CrossCutting.FileSystem;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Infrastructure.Packaging;
using MyClub.Scorer.Wpf.Messages;
using MyClub.Scorer.Wpf.Services.Handlers;
using MyClub.Scorer.Wpf.Services.Managers;
using MyClub.Scorer.Wpf.Settings;
using MyClub.Scorer.Wpf.ViewModels.Export;
using MyClub.Scorer.Wpf.ViewModels.Import;
using MyClub.Scorer.Wpf.ViewModels.Shell;
using MyClub.Scorer.Wpf.Views.Shell;
using MyClub.UserContext.Infrastructure.Authentication.Registry;
using MyNet.UI.Busy;
using MyNet.UI.Busy.Models;
using MyNet.UI.Commands;
using MyNet.UI.Dialogs;
using MyNet.UI.Extensions;
using MyNet.UI.Locators;
using MyNet.UI.Navigation;
using MyNet.UI.Notifications;
using MyNet.UI.Services;
using MyNet.UI.Services.Handlers;
using MyNet.UI.Services.Providers;
using MyNet.UI.Theming;
using MyNet.UI.Toasting;
using MyNet.UI.ViewModels.FileHistory;
using MyNet.UI.ViewModels.Shell;
using MyNet.Utilities;
using MyNet.Utilities.Extensions;
using MyNet.Utilities.Geography.Extensions;
using MyNet.Utilities.IO.AutoSave;
using MyNet.Utilities.IO.FileHistory;
using MyNet.Utilities.Localization;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Mail;
using MyNet.Utilities.Messaging;
using MyNet.Utilities.Progress;
using MyNet.Wpf.Presentation.Views.Export;
using MyNet.Wpf.Presentation.Views.FileHistory;
using MyNet.Wpf.Presentation.Views.Import;
using MyNet.Wpf.Presentation.Views.List;
using MyNet.Wpf.Presentation.Views.Shell;

namespace MyClub.Scorer.Wpf.Services;

/// <summary>
/// Managed host of the application.
/// </summary>
internal class ApplicationHostService : IHostedService
{
    private readonly IPersistentPreferencesService _preferencesService;
    private readonly IUserAuthenticationService _userAuthenticationService;
    private readonly ITempService _tempService;
    private readonly IAutoSaveService _autoSaveService;
    private readonly IMailService _mailService;
    private readonly RecentFilesService _recentFilesService;
    private readonly RecentFilesManager _recentFilesManager;
    private readonly RecentFilesProvider _recentFilesProvider;
    private readonly ProjectService _projectService;
    private readonly ProjectCommandsService _projectCommandsService;
    private readonly List<Action> _afterLoadedActions = [];
    private readonly object _lock = new();

    public ApplicationHostService(
        IThemeService themeService,
        INavigationService navigationService,
        IToasterService toasterService,
        IDialogService dialogService,
        IViewModelResolver viewModelResolver,
        IViewModelLocator viewModelLocator,
        IViewResolver viewResolver,
        IViewLocator viewLocator,
        IBusyServiceFactory busyServiceFactory,
        IMessageBoxFactory messageBoxFactory,
        ICommandFactory commandFactory,
        IScheduler uiScheduler,
        ILogger logger,
        ProjectService projectService,
        INotificationsManager notificationsManager,
        IPersistentPreferencesService persistentPreferencesService,
        IUserAuthenticationService userAuthenticationService,
        IMailService mailService,
        IProgresser progresser,
        ITempService tempService,
        IAutoSaveService autoSaveService,
        TimeAndLanguageSettingsService languageSettingsService,
        ThemeSettingsService themeSettingsService,
        RecentFilesProvider recentFilesProvider,
        RecentFilesService recentFilesService,
        ProjectCommandsService projectCommandsService,
#pragma warning disable IDE0060 // Remove unused parameter (used by dependency injection to instanciate type)
        ConflictsManager conflictsManager,
#pragma warning restore IDE0060 // Remove unused parameter
        RecentFilesManager recentFilesManager,
        MailConnectionHandler mailConnectionHandler,
        FileNotificationHandler fileNotificationHandler,
        ConflictsValidationHandler conflictsValidationHandler,
        NoneVenueValidationHandler noneVenueValidationHandler)
    {
        _preferencesService = persistentPreferencesService;
        _userAuthenticationService = userAuthenticationService;
        _mailService = mailService;
        _recentFilesManager = recentFilesManager;
        _recentFilesService = recentFilesService;
        _projectService = projectService;
        _recentFilesProvider = recentFilesProvider;
        _projectCommandsService = projectCommandsService;
        _tempService = tempService;
        _autoSaveService = autoSaveService;

        // Initialize managers
        LogManager.Initialize(logger);
        ViewModelManager.Initialize(viewModelResolver, viewModelLocator);
        ViewManager.Initialize(viewResolver, viewLocator);
        ThemeManager.Initialize(themeService);
        NavigationManager.Initialize(navigationService, viewModelLocator);
        ToasterManager.Initialize(toasterService);
        DialogManager.Initialize(dialogService, messageBoxFactory, viewResolver, viewLocator, viewModelLocator);
        BusyManager.Initialize(busyServiceFactory);
        ProgressManager.Initialize(progresser);
        CommandsManager.Initialize(commandFactory);
        MyNet.UI.Threading.Scheduler.Initialize(uiScheduler);
        AppBusyManager.Initialize(busyServiceFactory);
        notificationsManager.AddHandler(fileNotificationHandler)
                            .AddHandler(mailConnectionHandler)
                            .AddHandler(conflictsValidationHandler)
                            .AddHandler(noneVenueValidationHandler);

        progresser.Subscribe(new Progress<(double value, IEnumerable<ProgressMessage> messages, Action? cancelAction, bool canCancel)>(y =>
        {
            var progressionBusy = AppBusyManager.MainBusyService.GetCurrent<ProgressionBusy>();

            if (progressionBusy is null) return;

            lock (_lock)
            {
                var messages = y.messages.Where(x => !string.IsNullOrEmpty(x.Message))
                                         .Select(x =>
                                         {
                                             var str = x.Message.Translate() ?? x.Message;
                                             return x.Parameters is not null && x.Parameters.Length > 0 ? str.FormatWith(x.Parameters) : str;
                                         }).ToList();
                progressionBusy.Value = y.value;
                progressionBusy.Title = messages.FirstOrDefault() ?? string.Empty;
                progressionBusy.Messages = messages.TakeLast(messages.Count - 1).ToList();
                progressionBusy.CancelAction = y.cancelAction;
                progressionBusy.CanCancel = y.canCancel;
            }
        }));

        // Resolve common views
        viewResolver.Register<AboutViewModel, AboutView>();
        viewResolver.Register<PreferencesViewModel, PreferencesView>();
        viewResolver.Register<DisplayViewModel, MyNet.Wpf.Presentation.Views.Shell.DisplayView>();
        viewResolver.Register<ViewModels.Shell.TimeAndLanguageViewModel, TimeAndLanguageView>();
        viewResolver.Register<NotificationsViewModel, NotificationsView>();
        viewResolver.Register<RecentFilesViewModel, RecentFilesView>();
        viewResolver.Register<TeamsExportViewModel, FileExportByColumnsView>();
        viewResolver.Register<StadiumsExportViewModel, FileExportByColumnsView>();
        viewResolver.Register<TeamsImportBySourcesDialogViewModel, ImportBySourcesDialogView>();
        viewResolver.Register<StadiumsImportBySourcesDialogViewModel, ImportBySourcesDialogView>();
        viewResolver.Register<StadiumsImportDialogViewModel, SelectionDialogView>();

        // Translations
        TranslationService.RegisterResources(nameof(CountryResources), CountryResources.ResourceManager);
        TranslationService.RegisterResources(nameof(MyClubEnumsResources), MyClubEnumsResources.ResourceManager);
        TranslationService.RegisterResources(nameof(MyClubResources), MyClubResources.ResourceManager);

        // Settings
        languageSettingsService.Reload();
        themeSettingsService.Reload();

        LogStartApplication();
    }

    /// <summary>
    /// Triggered when the application host is ready to start the service.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var splashScreenViewModel = new SplashScreenViewModel();

        splashScreenViewModel.AddTasks(new (string message, Func<Task> task)[]
        {
            (MyClubResources.Authentication, AuthenticateAsync),
            (MyClubResources.SettingsValidation, ValidateSettingsAsync),
            (MyClubResources.LoadingRecentFiles, LoadRecentFilesAsync)
        });

        var filename = Environment.GetCommandLineArgs().Length == 2 ? Environment.GetCommandLineArgs()[1] : null;
        if (!string.IsNullOrEmpty(filename) && File.Exists(filename) && filename.IsScproj())
            splashScreenViewModel.AddTask(() => MyClubResources.OpeningProjectX.FormatWith(filename), () => LoadProjectAsync(filename), () => true);
        else if (AppSettings.Default.OpenLastProjectOnStart)
            splashScreenViewModel.AddTask(() => MyClubResources.OpeningProjectX.FormatWith(GetLastRecentFile()!.Path), () => LoadProjectAsync(GetLastRecentFile()!.Path), CanLoadLastProject);

        // Show Splash Screen
        var splashScreen = new SplashScreen(splashScreenViewModel);
        splashScreen.Closed += OnWindowClosed;
        splashScreen.Show();

        using (LogManager.MeasureTime("Initializing application", TraceLevel.Debug))
            await splashScreenViewModel.ExecuteAsync(failedCallback: e =>
            {
                LogManager.Fatal(e);
                _afterLoadedActions.Add(() => e.ShowInToaster());
            }).ConfigureAwait(false);

        NavigationCommandsService.NavigateToHomePage();

        MyNet.UI.Threading.Scheduler.GetUIOrCurrent().Schedule(() =>
        {
            // Close spash screen
            splashScreen.Closed -= OnWindowClosed;
            splashScreen.Close();

            var window = new MainWindow();
            System.Windows.Application.Current.MainWindow = window;
            window.Closed += OnWindowClosed;
            window.Closing += OnMainWindowClosingAsync;
            window.Show();

            _afterLoadedActions.ForEach(x => x.Invoke());
        });

        await Task.CompletedTask;
    }

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _preferencesService.Save();
        ((AutoSaveService)_autoSaveService).Clean();
        _tempService.Delete();
        LogEndApplication();
        await Task.CompletedTask;
    }

    private void OnWindowClosed(object? sender, EventArgs _) => Shutdown();

    private static void Shutdown(int exitCode = 0) => System.Windows.Application.Current.Shutdown(exitCode);

    #region Initialization

    private async Task AuthenticateAsync()
    {
        await Task.Run(_userAuthenticationService.Authenticate).ConfigureAwait(false);

        LogManager.Info($"User connected : {_userAuthenticationService.CurrentPrincipal.Name} ({_userAuthenticationService.CurrentPrincipal.DisplayName})");
    }

    private async Task LoadProjectAsync(string filename)
    {
        try
        {
            var project = await _projectService.LoadAsync(filename).ConfigureAwait(false);

            if (project is not null)
                _recentFilesManager.Add(project.Name, filename);

            _afterLoadedActions.Add(() => ToasterManager.ShowSuccess(MyClubResources.ProjectXLoadedSuccess.FormatWith(filename)));
        }
        catch (Exception e)
        {
            _afterLoadedActions.Add(() => e.ShowInToaster(true, false));
        }
    }

    private RecentFile? GetLastRecentFile() => _recentFilesService.GetLastRecentFile();

    private bool CanLoadLastProject() => GetLastRecentFile() is RecentFile fileInfo && File.Exists(fileInfo.Path) && fileInfo.Path.IsScproj();

    private async Task LoadRecentFilesAsync() => await Task.Run(_recentFilesProvider.Reload).ConfigureAwait(false);

    private async Task ValidateSettingsAsync()
    {
        if (AppSettings.Default.CheckMailConnectionOnStart)
        {
            using (LogManager.MeasureTime("Checking Mail Connection", TraceLevel.Debug))
                if (!await _mailService.CanConnectAsync().ConfigureAwait(false))
                {
                    LogManager.Warning("Connection to mail server failed");
                    _afterLoadedActions.Add(() => Messenger.Default.Send(new MailConnectionCheckedMessage(false)));
                }
        }
    }

    #endregion Initialization

    private static void LogEndApplication()
    {
        var assembly = Assembly.GetEntryAssembly();

        LogManager.Info($"User Settings saved");
        LogManager.Info($"**************** End Application {assembly?.GetName().Name} - Version {assembly?.GetName().Version?.ToString()} ****************");
    }

    private static void LogStartApplication()
    {
        var assembly = Assembly.GetEntryAssembly();

        LogManager.Info($"**************** Start Application {assembly?.GetName().Name} - Version {assembly?.GetName().Version?.ToString()} ****************");
        LogManager.Info($"Language : {GlobalizationService.Current.Culture.DisplayName} | Time zone : {GlobalizationService.Current.TimeZone.DisplayName}");
    }

    private async void OnMainWindowClosingAsync(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        if (await _projectCommandsService.EnsureProjectIsSavedAsync().ConfigureAwait(false))
        {
            MyNet.UI.Threading.Scheduler.GetUIOrCurrent().Schedule(() => Shutdown());
        }
    }
}
