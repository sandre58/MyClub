// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyClub.CrossCutting.FileSystem;
using MyClub.Domain.Services;
using MyClub.Scorer.Application.Contracts;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Factories;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.PersonAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyClub.Scorer.Infrastructure.Packaging;
using MyClub.Scorer.Infrastructure.Packaging.Services;
using MyClub.Scorer.Infrastructure.Repositories;
using MyClub.Scorer.Wpf.Configuration;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Factories;
using MyClub.Scorer.Wpf.Services.Handlers;
using MyClub.Scorer.Wpf.Services.Managers;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.Settings;
using MyClub.Scorer.Wpf.ViewModels.BracketPage;
using MyClub.Scorer.Wpf.ViewModels.BuildAssistant;
using MyClub.Scorer.Wpf.ViewModels.Edition;
using MyClub.Scorer.Wpf.ViewModels.Export;
using MyClub.Scorer.Wpf.ViewModels.HomePage;
using MyClub.Scorer.Wpf.ViewModels.Import;
using MyClub.Scorer.Wpf.ViewModels.PastPositionsPage;
using MyClub.Scorer.Wpf.ViewModels.RankingPage;
using MyClub.Scorer.Wpf.ViewModels.SchedulePage;
using MyClub.Scorer.Wpf.ViewModels.SchedulingAssistant;
using MyClub.Scorer.Wpf.ViewModels.Shell;
using MyClub.Scorer.Wpf.ViewModels.StadiumsPage;
using MyClub.Scorer.Wpf.ViewModels.TeamsPage;
using MyClub.UserContext.Application.Services;
using MyClub.UserContext.Domain.UserAggregate;
using MyClub.UserContext.Infrastructure.Authentication.Registry;
using MyNet.UI.Busy;
using MyNet.UI.Commands;
using MyNet.UI.Dialogs;
using MyNet.UI.Dialogs.Settings;
using MyNet.UI.Locators;
using MyNet.UI.Navigation;
using MyNet.UI.Notifications;
using MyNet.UI.Resources;
using MyNet.UI.Services;
using MyNet.UI.Services.Handlers;
using MyNet.UI.Services.Providers;
using MyNet.UI.Theming;
using MyNet.UI.Toasting;
using MyNet.UI.Toasting.Settings;
using MyNet.UI.ViewModels.FileHistory;
using MyNet.Utilities;
using MyNet.Utilities.Encryption;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Extensions;
using MyNet.Utilities.Generator;
using MyNet.Utilities.Google.Maps;
using MyNet.Utilities.IO.AutoSave;
using MyNet.Utilities.IO.FileHistory;
using MyNet.Utilities.IO.FileHistory.Registry;
using MyNet.Utilities.IO.Registry;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Logging.NLog;
using MyNet.Utilities.Mail;
using MyNet.Utilities.Mail.Smtp;
using MyNet.Utilities.Progress;
using MyNet.Wpf.Busy;
using MyNet.Wpf.Commands;
using MyNet.Wpf.Dialogs;
using MyNet.Wpf.Presentation.Services;
using MyNet.Wpf.Schedulers;
using MyNet.Wpf.Theming;
using MyNet.Wpf.Toasting;

namespace MyClub.Scorer.Wpf
{
    public sealed partial class App
    {
        private static readonly IHost Host = Microsoft.Extensions.Hosting.Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, configurationBuilder) => configurationBuilder.SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!)
                                                                                              .AddJsonFile("config/appsettings.json", optional: false, reloadOnChange: true)
                                                                                              .AddEnvironmentVariables())
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();

                Logger.LoadConfiguration(string.Concat(Directory.GetCurrentDirectory(), "/config/NLog.config"));
                logging.AddProvider(new LoggerProvider());
            })
            .ConfigureServices((context, services) => services

                // App Host
                .AddHostedService<ApplicationHostService>()

                // UI Services
                .AddSingleton<MyNet.Utilities.Logging.ILogger, Logger>()
                .AddSingleton<IViewModelResolver, ViewModelResolver>()
                .AddSingleton<IViewModelLocator, ViewModelLocator>(x => new ViewModelLocator(x))
                .AddSingleton<IViewLocator, ViewLocator>()
                .AddSingleton<IViewResolver, ViewResolver>()
                .AddSingleton<IThemeService, ThemeService>()
                .AddSingleton<INavigationService, NavigationCommandsService>()
                .AddSingleton<IToasterService, ToasterService>()
                .AddSingleton<IDialogService, OverlayDialogService>()
                .AddSingleton<IProgresser, Progresser>()
                .AddScoped<IBusyServiceFactory, BusyServiceFactory>()
                .AddScoped<IMessageBoxFactory, MessageBoxFactory>()
                .AddScoped<IScheduler, WpfScheduler>(_ => WpfScheduler.Current)
                .AddScoped<ICommandFactory, WpfCommandFactory>()

                // Global Services
                .AddSingleton(x => new PluginsService(Path.GetFullPath(x.GetRequiredService<ScorerConfiguration>().Plugins.Directory), x))
                .AddSingleton<IAutoSaveService, AutoSaveService>(x => new AutoSaveService(x.GetRequiredService<ProjectInfoProvider>(),
                                                                                          x.GetRequiredService<ProjectService>(),
                                                                                          x.GetRequiredService<ITempService>(),
                                                                                          x.GetRequiredService<IRecentFileRepository>(),
                                                                                          AppSettings.Default.IsAutoSaveEnabled,
                                                                                          AppSettings.Default.AutoSaveInterval))
                .AddSingleton(x => new RegistryAuthenticationService(x.GetRequiredService<ScorerConfiguration>().Authentication.Registry))
                .AddSingleton<IEncryptionService, AesEncryptionService>(x => new AesEncryptionService(GetOrCreateEncryptionKey()))
                .AddSingleton<IUserAuthenticationService>(x => x.GetRequiredService<RegistryAuthenticationService>())
                .AddSingleton<INotificationsManager, NotificationsManager>()
                .AddScoped<UserService>()
                .AddScoped<IAuditService, AuditService>()
                .AddScoped<IAppCommandsService, AppCommandsService>()
                .AddScoped<IMailServiceFactory, MailServiceFactory>()
                .AddScoped<IEmailFactory, UserEmailFactory>()
                .AddScoped(x => x.GetRequiredService<IMailServiceFactory>().Create(new SmtpClientOptions
                {
                    Server = SmtpSettings.Default.Server,
                    Port = SmtpSettings.Default.Port,
                    Password = x.GetRequiredService<IEncryptionService>().Decrypt(SmtpSettings.Default.Password),
                    RequiresAuthentication = SmtpSettings.Default.RequiresAuthentication,
                    User = SmtpSettings.Default.Username,
                    UseSsl = SmtpSettings.Default.UseSsl
                }))
                .AddScoped<ILocationService>(x => new GoogleLocationService(ScorerConfiguration.GoogleApiKey, true))
                .AddScoped<IRegistryService, RegistryService>()
                .AddScoped<RecentFilesService>()
                .AddScoped<IRecentFileRepository>(x => new RecentFileRepository(
                    x.GetRequiredService<IRegistryService>(),
                    x.GetRequiredService<ScorerConfiguration>().RecentFiles.Registry,
                    [ScprojFileExtensionInfo.Scproj.Extensions[0][1..]],
                    x.GetRequiredService<ScorerConfiguration>().RecentFiles.Max))

                // Domain Services
                .AddScoped(CreateProjectFactory)
                .AddScoped<IUserRepository>(x => x.GetRequiredService<RegistryAuthenticationService>())
                .AddSingleton<IProjectRepository, ProjectRepository>()
                .AddSingleton<ILeagueRepository, LeagueRepository>()
                .AddScoped<ITeamRepository, TeamRepository>()
                .AddScoped<IStadiumRepository, StadiumRepository>()
                .AddScoped<IPlayerRepository, PlayerRepository>()
                .AddScoped<IManagerRepository, ManagerRepository>()
                .AddScoped<IMatchdayRepository, MatchdayRepository>()
                .AddScoped<IMatchRepository, MatchRepository>()
                .AddScoped<ISchedulingDomainService, SchedulingDomainService>()

                // Application Services
                .AddScoped<ProjectService>()
                .AddScoped<AddressService>()
                .AddScoped<TeamService>()
                .AddScoped<StadiumService>()
                .AddScoped<PlayerService>()
                .AddScoped<ManagerService>()
                .AddScoped<MatchdayService>()
                .AddScoped<MatchService>()
                .AddScoped<LeagueService>()
                .AddScoped<AvailibilityCheckingService>()

                // Infrastructure Service
                .AddSingleton<ITempService>(x => new TempService(x.GetRequiredService<ScorerConfiguration>().TempDirectory))
                .AddScoped<IReadService, ReadService>()
                .AddScoped<IWriteService, WriteService>()

                // Presentation services
                .AddScoped<RecentFilesManager>()
                .AddScoped<IPersistentPreferencesService, SettingsService>()
                .AddScoped<TimeAndLanguageSettingsService>()
                .AddScoped<ThemeSettingsService>()
                .AddScoped<AppSettingsService>()
                .AddScoped<IRecentFileCommandsService, RecentFileCommandsService>()
                .AddScoped<ProjectCommandsService>()
                .AddScoped<CompetitionCommandsService>()
                .AddScoped<TeamPresentationService>()
                .AddScoped<StadiumPresentationService>()
                .AddScoped<PersonPresentationService>()
                .AddScoped<MatchdayPresentationService>()
                .AddScoped<MatchPresentationService>()
                .AddScoped<LeaguePresentationService>()

                // Managers
                .AddSingleton<ConflictsManager>()

                // Presentation source Providers
                .AddSingleton<RecentFilesProvider>()
                .AddSingleton<ProjectInfoProvider>()
                .AddSingleton<CompetitionInfoProvider>()
                .AddSingleton<TeamsProvider>()
                .AddSingleton<StadiumsProvider>()
                .AddSingleton<MatchdaysProvider>()
                .AddSingleton<MatchesProvider>()

                // Notifications handlers
                .AddSingleton<FileNotificationHandler>()
                .AddSingleton<MailConnectionHandler>()
                .AddSingleton<ConflictsValidationHandler>()
                .AddSingleton<NoneVenueValidationHandler>()

                // ViewModels
                .AddSingleton<MainWindowViewModel>()
                .AddSingleton<RecentFilesViewModel>()
                // ViewModels - Pages
                .AddSingleton<HomePageViewModel>()
                .AddSingleton<TeamsPageViewModel>()
                .AddSingleton<StadiumsPageViewModel>()
                .AddSingleton<SchedulePageViewModel>()
                .AddSingleton<RankingPageViewModel>()
                .AddSingleton<BracketPageViewModel>()
                .AddSingleton<PastPositionsPageViewModel>()
                // ViewModels - Edition dialogs
                .AddSingleton<SettingsEditionViewModel>()
                .AddSingleton<UserEditionViewModel>()
                .AddSingleton<ProjectEditionViewModel>()
                .AddSingleton<TeamEditionViewModel>()
                .AddSingleton<StadiumEditionViewModel>()
                .AddSingleton<PlayerEditionViewModel>()
                .AddSingleton<ManagerEditionViewModel>()
                .AddSingleton<MatchdayEditionViewModel>()
                .AddSingleton<MatchdaysEditionViewModel>()
                .AddSingleton<MatchEditionViewModel>()
                .AddSingleton<RankingRulesEditionViewModel>()
                .AddSingleton<SchedulingParametersEditionViewModel>()
                .AddSingleton<SchedulingAssistantViewModel>()
                .AddSingleton<LeagueBuildAssistantViewModel>()
                // ViewModels - Other dialogs
                .AddSingleton<StadiumsExportViewModel>()
                .AddSingleton<TeamsExportViewModel>()
                .AddSingleton(x => new StadiumsImportBySourcesDialogViewModel(x.GetRequiredService<ProjectInfoProvider>(), new StadiumsImportBySourcesProvider(x.GetRequiredService<PluginsService>(), (y, z) => x.GetRequiredService<StadiumService>().GetSimilarStadiums(y, z).Any())))
                .AddSingleton(x => new TeamsImportBySourcesDialogViewModel(x.GetRequiredService<ProjectInfoProvider>(), new TeamsImportBySourcesProvider(x.GetRequiredService<PluginsService>(), y => x.GetRequiredService<TeamService>().GetSimilarTeams(y).Any())))

                // Configuration
                .Configure<ScorerConfiguration>(context.Configuration)
                .AddScoped<ScorerConfiguration>()

            ).Build();

        private static IProjectFactory CreateProjectFactory(IServiceProvider serviceProvider)
        {
            var configuration = serviceProvider.GetRequiredService<ScorerConfiguration>();
            var mockDirectory = Path.GetFullPath(configuration.Mock.Directory);

            if (Directory.Exists(mockDirectory) && !string.IsNullOrEmpty(configuration.Mock.FactoryName))
            {
                var factoryDllPath = Path.Combine(mockDirectory, configuration.Mock.FactoryName, $"{configuration.Mock.FactoryName}.dll");
                var assembly = DllLoadContext.LoadAssemblyFromDll(factoryDllPath);

                if (assembly is not null)
                {
                    var type = Array.Find(assembly.GetTypes(), x => x.IsAssignableTo(typeof(IProjectFactory)));

                    if (type is not null)
                    {
                        var instance = (IProjectFactory?)ActivatorUtilities.CreateInstance(serviceProvider, type);

                        if (instance is not null) return instance;
                    }
                }
            }

            return new ProjectFactory(serviceProvider.GetRequiredService<IAuditService>());
        }

        static App() => AppDomain.CurrentDomain.UnhandledException += async (sender, e) => await OnAppDomainUnhandledExceptionAsync(e).ConfigureAwait(false);

        public App() => DispatcherUnhandledException += async (sender, e) => await OnDispatcherUnhandledExceptionAsync(e).ConfigureAwait(false);

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Start Main Application
            await Host.StartAsync();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            await Host.StopAsync();

            Host.Dispose();
        }

        private static async Task OnAppDomainUnhandledExceptionAsync(UnhandledExceptionEventArgs e) => await ShowExceptionAsync((Exception)e.ExceptionObject).ConfigureAwait(false);

        private static async Task OnDispatcherUnhandledExceptionAsync(DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            await ShowExceptionAsync(e.Exception).ConfigureAwait(false);
        }

        private static async Task ShowExceptionAsync(Exception e)
        {
            switch (e.InnerException ?? e)
            {
                case TranslatableException exception:
                    LogManager.Error(exception);
                    ToasterManager.ShowError(exception.Parameters is not null ? exception.ResourceKey.Translate()?.FormatWith(exception.Parameters) : exception.ResourceKey.Translate(), ToastClosingStrategy.AutoClose);
                    break;

                default:
                    var innerException = e.InnerException ?? e;
                    LogManager.Fatal(innerException);

                    // If Binding error
                    if (innerException is ResourceReferenceKeyNotFoundException or XamlParseException) return;

                    if (await DialogManager.ShowErrorAsync(MessageResources.UnexpectedXError.FormatWith(e.Message), buttons: MessageBoxResultOption.OkCancel).ConfigureAwait(false) == MyNet.UI.Dialogs.MessageBoxResult.Cancel)
                        MyNet.UI.Threading.Scheduler.UI.Schedule(() => Current.Shutdown(-1));
                    break;
            }
        }

        private static byte[] GetOrCreateEncryptionKey()
        {
            if (string.IsNullOrEmpty(SmtpSettings.Default.EncryptionKey))
            {
                SmtpSettings.Default.EncryptionKey = RandomGenerator.String2(32, "azertyuiopqsdfghjklmwxcvbn?.§,;:!0123456789+-*");
                SmtpSettings.Default.Save();
            }

            return Encoding.ASCII.GetBytes(SmtpSettings.Default.EncryptionKey);
        }
    }
}
