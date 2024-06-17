// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
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
using MyClub.Application.Identity.Services;
using MyClub.CrossCutting.FileSystem;
using MyClub.Domain.Services;
using MyClub.Teamup.Application.Contracts;
using MyClub.Teamup.Application.Deferrers;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Domain.CycleAggregate;
using MyClub.Teamup.Domain.Factories;
using MyClub.Teamup.Domain.HolidaysAggregate;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Domain.MyTeamAggregate;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.SendedMailAggregate;
using MyClub.Teamup.Domain.SquadAggregate;
using MyClub.Teamup.Domain.StadiumAggregate;
using MyClub.Teamup.Domain.TacticAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyClub.Teamup.Domain.TrainingAggregate;
using MyClub.Teamup.Infrastructure.Packaging;
using MyClub.Teamup.Infrastructure.Packaging.Services;
using MyClub.Teamup.Infrastructure.Repositories;
using MyClub.Teamup.Wpf.Configuration;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Factories;
using MyClub.Teamup.Wpf.Services.Handlers;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.Settings;
using MyClub.Teamup.Wpf.ViewModels.CalendarPage;
using MyClub.Teamup.Wpf.ViewModels.CommunicationPage;
using MyClub.Teamup.Wpf.ViewModels.CompetitionPage;
using MyClub.Teamup.Wpf.ViewModels.CompetitionsPage;
using MyClub.Teamup.Wpf.ViewModels.Edition;
using MyClub.Teamup.Wpf.ViewModels.Export;
using MyClub.Teamup.Wpf.ViewModels.HomePage;
using MyClub.Teamup.Wpf.ViewModels.Import;
using MyClub.Teamup.Wpf.ViewModels.MatchPage;
using MyClub.Teamup.Wpf.ViewModels.MedicalCenterPage;
using MyClub.Teamup.Wpf.ViewModels.PlayerPage;
using MyClub.Teamup.Wpf.ViewModels.RosterPage;
using MyClub.Teamup.Wpf.ViewModels.Shell;
using MyClub.Teamup.Wpf.ViewModels.TacticPage;
using MyClub.Teamup.Wpf.ViewModels.TrainingPage;
using MyClub.Teamup.Wpf.ViewModels.TrainingSessionPage;
using MyClub.UserContext.Application.Services;
using MyClub.UserContext.Domain.UserAggregate;
using MyClub.UserContext.Infrastructure.Authentication.Registry;
using MyNet.Humanizer;
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

namespace MyClub.Teamup.Wpf
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

                // Configuration
                .Configure<TeamupConfiguration>(context.Configuration)
                .AddScoped<TeamupConfiguration>()

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
                .AddSingleton(x => new PluginsService(Path.GetFullPath(x.GetRequiredService<TeamupConfiguration>().Plugins.Directory), x))
                .AddSingleton<IAutoSaveService, AutoSaveService>(x => new AutoSaveService(x.GetRequiredService<ProjectInfoProvider>(),
                                                                                          x.GetRequiredService<IWriteService>(),
                                                                                          x.GetRequiredService<ITempService>(),
                                                                                          x.GetRequiredService<IRecentFileRepository>(),
                                                                                          AppSettings.Default.IsAutoSaveEnabled,
                                                                                          AppSettings.Default.AutoSaveInterval))
                .AddSingleton<IEncryptionService, AesEncryptionService>(x => new AesEncryptionService(GetOrCreateEncryptionKey()))
                .AddSingleton(x => new RegistryAuthenticationService(x.GetRequiredService<TeamupConfiguration>().Authentication.Registry))
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
                .AddScoped<ILocationService>(x => new GoogleLocationService(TeamupConfiguration.GoogleApiKey, true))
                .AddScoped<IRegistryService, RegistryService>()
                .AddScoped<RecentFilesService>()
                .AddScoped<IRecentFileRepository>(x => new RecentFileRepository(
                    x.GetRequiredService<IRegistryService>(),
                    x.GetRequiredService<TeamupConfiguration>().RecentFiles.Registry,
                    [TmprojFileExtensionInfo.Tmproj.Extensions[0][1..]],
                    x.GetRequiredService<TeamupConfiguration>().RecentFiles.Max))

                // Domain Services
                .AddScoped(CreateProjectFactory)
                .AddScoped<IUserRepository>(x => x.GetRequiredService<RegistryAuthenticationService>())
                .AddScoped<IProjectRepository, ProjectRepository>()
                .AddScoped<ISquadPlayerRepository, SquadPlayerRepository>()
                .AddScoped<IInjuryRepository, InjuryRepository>()
                .AddScoped<IAbsenceRepository, AbsenceRepository>()
                .AddScoped<IHolidaysRepository, HolidaysRepository>()
                .AddScoped<ITacticRepository, TacticRepository>()
                .AddScoped<ISendedMailRepository, SendedMailRepository>()
                .AddScoped<ICycleRepository, CycleRepository>()
                .AddScoped<ITrainingSessionRepository, TrainingSessionRepository>()
                .AddScoped<IStadiumRepository, StadiumRepository>()
                .AddScoped<ITeamRepository, TeamRepository>()
                .AddScoped<ICompetitionSeasonRepository, CompetitionSeasonRepository>()
                .AddScoped<IMatchRepository, MatchRepository>()
                .AddScoped<IMatchdayRepository, MatchdayRepository>()
                .AddScoped<IRoundRepository, RoundRepository>()
                .AddScoped<IMyTeamDomainService, MyTeamDomainService>()

                // Application Services
                .AddSingleton<InjuriesStatisticsRefreshDeferrer>()
                .AddSingleton<TrainingStatisticsRefreshDeferrer>()
                .AddScoped<ProjectService>()
                .AddScoped<PlayerService>()
                .AddScoped<InjuryService>()
                .AddScoped<AbsenceService>()
                .AddScoped<HolidaysService>()
                .AddScoped<SendedMailService>()
                .AddScoped<CycleService>()
                .AddScoped<TrainingSessionService>()
                .AddScoped<TacticService>()
                .AddScoped<StadiumService>()
                .AddScoped<TeamService>()
                .AddScoped<AddressService>()
                .AddScoped<TeamService>()
                .AddScoped<CompetitionService>()
                .AddScoped<MatchService>()
                .AddScoped<MatchdayService>()
                .AddScoped<RoundService>()

                // Infrastructure Service
                .AddSingleton<ITempService>(x => new TempService(x.GetRequiredService<TeamupConfiguration>().TempDirectory))
                .AddScoped<IReadService, ReadService>()
                .AddScoped<IWriteService, WriteService>()

                // Presentation services
                .AddScoped<RecentFilesManager>()
                .AddScoped<IPersistentPreferencesService, SettingsService>()
                .AddScoped<LanguageSettingsService>()
                .AddScoped<ThemeSettingsService>()
                .AddScoped<AppSettingsService>()
                .AddScoped<IRecentFileCommandsService, RecentFileCommandsService>()
                .AddScoped<ProjectCommandsService>()
                .AddScoped<MailCommandsService>()
                .AddScoped<PlayerPresentationService>()
                .AddScoped<SendedMailPresentationService>()
                .AddScoped<TrainingSessionPresentationService>()
                .AddScoped<HolidaysPresentationService>()
                .AddScoped<CyclePresentationService>()
                .AddScoped<TacticPresentationService>()
                .AddScoped<FriendlyPresentationService>()
                .AddScoped<LeaguePresentationService>()
                .AddScoped<CupPresentationService>()
                .AddScoped<CompetitionPresentationService>()
                .AddScoped<MatchPresentationService>()
                .AddScoped<MatchdayPresentationService>()
                .AddScoped<RoundPresentationService>()
                .AddScoped<TeamPresentationService>()
                .AddScoped<StadiumPresentationService>()

                // Presentation source Providers
                .AddSingleton<RecentFilesProvider>()
                .AddSingleton<PlayersProvider>()
                .AddSingleton<MainItemsProvider>()
                .AddSingleton<TrainingSessionsProvider>()
                .AddSingleton<CyclesProvider>()
                .AddSingleton<HolidaysProvider>()
                .AddSingleton<SendedMailsProvider>()
                .AddSingleton<InjuriesProvider>()
                .AddSingleton<TacticsProvider>()
                .AddSingleton<StadiumsProvider>()
                .AddSingleton<CompetitionsProvider>()
                .AddSingleton<TeamsProvider>()
                .AddSingleton<ProjectInfoProvider>()
                .AddSingleton<NavigableItemsProvider>()
                .AddSingleton<ActionsProvider>()
                .AddSingleton<PlayersImportBySourcesProvider>()
                .AddSingleton<TeamsImportBySourcesProvider>()
                .AddSingleton<CompetitionsImportBySourcesProvider>()

                // Notifications handlers
                .AddSingleton<MailConnectionHandler>()
                .AddSingleton<FileNotificationHandler>()
                .AddSingleton<TeamsValidationHandler>()

                // ViewModels
                .AddSingleton<MainWindowViewModel>()
                .AddSingleton<RecentFilesViewModel>()
                // ViewModels - Pages
                .AddSingleton<HomePageViewModel>()
                .AddSingleton<RosterPageViewModel>()
                .AddSingleton<PlayerPageViewModel>()
                .AddSingleton<TrainingPageViewModel>()
                .AddSingleton<TrainingSessionPageViewModel>()
                .AddSingleton<MedicalCenterPageViewModel>()
                .AddSingleton<CommunicationPageViewModel>()
                .AddSingleton<TacticPageViewModel>()
                .AddSingleton<CompetitionsPageViewModel>()
                .AddSingleton<LeaguePageViewModel>()
                .AddSingleton<CupPageViewModel>()
                .AddSingleton<FriendlyPageViewModel>()
                .AddSingleton<CalendarPageViewModel>()
                .AddSingleton<MatchPageViewModel>()
                // ViewModels - Edition dialogs
                .AddSingleton<SettingsEditionViewModel>()
                .AddSingleton<ProjectEditionViewModel>()
                .AddSingleton<UserEditionViewModel>()
                .AddSingleton<MyTeamsEditionViewModel>()
                .AddSingleton<PlayerEditionViewModel>()
                .AddSingleton<PlayersEditionViewModel>()
                .AddSingleton<TrainingSessionEditionViewModel>()
                .AddSingleton<TrainingAttendancesEditionViewModel>()
                .AddSingleton<TrainingSessionsAddViewModel>()
                .AddSingleton<TrainingSessionsEditionViewModel>()
                .AddSingleton<InjuryEditionViewModel>()
                .AddSingleton<AbsenceEditionViewModel>()
                .AddSingleton<HolidaysEditionViewModel>()
                .AddSingleton<CycleEditionViewModel>()
                .AddSingleton<FriendlyEditionViewModel>()
                .AddSingleton<LeagueEditionViewModel>()
                .AddSingleton<CupEditionViewModel>()
                .AddSingleton<TacticEditionViewModel>()
                .AddSingleton<StadiumEditionViewModel>()
                .AddSingleton<TeamEditionViewModel>()
                .AddSingleton<RankLabelEditionViewModel>()
                .AddSingleton<MatchdayEditionViewModel>()
                .AddSingleton<KnockoutEditionViewModel>()
                .AddSingleton<GroupStageEditionViewModel>()
                .AddSingleton<MatchesEditionViewModel>()
                .AddSingleton<MatchEditionViewModel>()
                // ViewModels - Other dialogs
                .AddSingleton<PlayersExportViewModel>()
                .AddSingleton<CompetitionsExportViewModel>()
                .AddSingleton<TeamsExportViewModel>()
                .AddSingleton<TeamsImportBySourcesDialogViewModel>()
                .AddSingleton<PlayersImportBySourcesDialogViewModel>()
                .AddSingleton<CompetitionsImportBySourcesDialogViewModel>()
            ).Build();

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

        private static byte[] GetOrCreateEncryptionKey()
        {
            if (string.IsNullOrEmpty(SmtpSettings.Default.EncryptionKey))
            {
                SmtpSettings.Default.EncryptionKey = RandomGenerator.String2(32, "azertyuiopqsdfghjklmwxcvbn?.§,;:!0123456789+-*");
                SmtpSettings.Default.Save();
            }

            return Encoding.ASCII.GetBytes(SmtpSettings.Default.EncryptionKey);
        }

        private static IProjectFactory CreateProjectFactory(IServiceProvider serviceProvider)
        {
            var configuration = serviceProvider.GetRequiredService<TeamupConfiguration>();
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
    }
}
