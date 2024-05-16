// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Application.Contracts;
using MyClub.Teamup.Application.Deferrers;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Messages;
using MyClub.Teamup.Domain.ClubAggregate;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.Randomize;
using MyClub.Teamup.Domain.Randomize.Extensions;
using MyClub.Teamup.Domain.SeasonAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Generator;
using MyNet.Utilities.Helpers;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Messaging;
using MyNet.Utilities.Progress;

namespace MyClub.Teamup.Application.Services
{
    public sealed class ProjectService(
        IProjectRepository projectRepository,
        IReadService readService,
        IWriteService writeService,
        IProjectFactory projectFactory,
        StadiumService stadiumService,
        InjuriesStatisticsRefreshDeferrer injuriesStatisticsRefreshDeferrer,
        TrainingStatisticsRefreshDeferrer trainingStatisticsRefreshDeferrer)
    {
        private readonly IProjectRepository _projectRepository = projectRepository;
        private readonly IProjectFactory _projectFactory = projectFactory;
        private readonly IReadService _readService = readService;
        private readonly IWriteService _writeService = writeService;
        private readonly StadiumService _stadiumService = stadiumService;
        private readonly InjuriesStatisticsRefreshDeferrer _injuriesStatisticsRefreshDeferrer = injuriesStatisticsRefreshDeferrer;
        private readonly TrainingStatisticsRefreshDeferrer _trainingStatisticsRefreshDeferrer = trainingStatisticsRefreshDeferrer;
        private static readonly double[] SubStepDefinitionsRandomize = [0.05, 0.05, 0.05, 0.05, 0.1, 0.1, 0.6];
        private static readonly double[] SubStepDefinitionsLoad = [0.8, 0.2];
        private static readonly double[] SubStepDefinitionsNew2 = [0.1, 0.9];
        private static readonly double[] SubStepDefinitionsNewRandomize = [0.25, 0.25, 0.5];
        private static readonly double[] SubStepDefinitionsNew = [0.5, 0.5];

        public async Task<Project> NewAsync(bool randomize = false, CancellationToken cancellationToken = default)
        {
            using (_trainingStatisticsRefreshDeferrer.Defer())
            using (ProgressManager.Start(randomize ? SubStepDefinitionsNewRandomize : SubStepDefinitionsNew))
            {
                Project project;

                using (LogManager.MeasureTime($"Create default project", TraceLevel.Debug))
                using (ProgressManager.Start(MyClubResources.ProgressGeneratingProject))
                    project = await _projectFactory.CreateAsync(cancellationToken).ConfigureAwait(false);

                _ = Load(project);

                if (randomize)
                    Randomize(project);

                return project;
            }
        }

        public Project New(ProjectMetadataDto properties, bool randomize = false)
        {
            Project project;

            using (_trainingStatisticsRefreshDeferrer.Defer())
            using (ProgressManager.Start(randomize ? SubStepDefinitionsNew2 : [1]))
            {
                using (LogManager.MeasureTime($"Create new project : {properties.Name}", TraceLevel.Debug))
                {
                    var club = new Club(properties.Club.OrThrow().Name.OrThrow())
                    {
                        AwayColor = properties.Club.OrThrow().AwayColor.OrEmpty(),
                        HomeColor = properties.Club.OrThrow().HomeColor.OrEmpty(),
                        Country = properties.Club.OrThrow().Country,
                        Logo = properties.Club.OrThrow().Logo
                    };
                    _stadiumService.AssignStadium(properties.Club.OrThrow().Stadium, x => club.Stadium = x);
                    club.AddTeam(properties.Category.OrThrow());

                    project = new Project(properties.Name.OrThrow(),
                                          club,
                                          properties.Category.OrThrow(),
                                          new Season(properties.Season.OrThrow().StartDate, properties.Season.OrThrow().EndDate),
                                          properties.Color.OrThrow(),
                                          properties.Image)
                    {
                        Preferences = new ProjectPreferences(properties.TrainingStartTime, properties.TrainingDuration),
                        MainTeam = club.Teams[0]
                    };

                    _ = Load(project);
                }

                if (randomize)
                    Randomize(project);

                return project;
            }
        }

        public void Update(ProjectMetadataDto properties)
        {
            _projectRepository.Update(x =>
            {
                x.MainTeam = properties.MainTeamId.HasValue ? x.Club.Teams.FirstOrDefault(x => x.Id == properties.MainTeamId.Value) : null;
                x.Name = properties.Name.OrThrow();
                x.Color = properties.Color.OrThrow();
                x.Club.Name = properties.Club.OrThrow().Name.OrThrow();
                x.Club.ShortName = properties.Club.OrThrow().Name.OrThrow();
                x.Image = properties.Image;
                x.Club.Country = properties.Club.OrThrow().Country;
                x.Club.HomeColor = properties.Club.OrThrow().HomeColor.OrThrow();
                x.Club.AwayColor = properties.Club.OrThrow().AwayColor.OrThrow();
                x.Preferences = new(properties.TrainingStartTime, properties.TrainingDuration);

                _stadiumService.AssignStadium(properties.Club.OrThrow().Stadium, y => x.Club.Stadium = y);
            });

            Messenger.Default.Send(new StadiumsChangedMessage());
        }

        public Project Load(Project project)
        {
            LoadProject(project);

            return project;
        }

        public async Task<Project> LoadTemplateAsync(string filename, CancellationToken? token = null) => await LoadAsync(filename, true, token);

        public async Task<Project> LoadAsync(string filename, CancellationToken? token = null) => await LoadAsync(filename, false, token);

        private async Task<Project> LoadAsync(string filename, bool isTemplate, CancellationToken? token = null)
        {
            if (string.IsNullOrEmpty(filename))
                throw new InvalidOperationException("File path is empty.");

            Project project;
            using (LogManager.MeasureTime($"Read file : {filename}", TraceLevel.Debug))
            using (ProgressManager.Start(SubStepDefinitionsLoad, nameof(MyClubResources.ProgressReadingFile), filename))
                project = await _readService.ReadAsync(filename, token).ConfigureAwait(false);

            LoadProject(project, !isTemplate ? filename : null);

            return project;
        }

        private void LoadProject(Project project, string? filepath = null)
        {
            using (_injuriesStatisticsRefreshDeferrer.Defer())
            using (_trainingStatisticsRefreshDeferrer.Defer())
            using (LogManager.MeasureTime($"Load project : {project.Name}", TraceLevel.Trace))
            using (ProgressManager.StartUncancellable(MyClubResources.ProgressLoadingProject))
            {
                Messenger.Default.Send(new CurrentProjectChangingMessage(project, filepath));

                _projectRepository.Load(project);

                Messenger.Default.Send(new CurrentProjectChangedMessage(project, filepath));
            }
        }

        public void Close()
        {
            Messenger.Default.Send(new CurrentProjectChangingMessage());

            _projectRepository.Clear();

            Messenger.Default.Send(new CurrentProjectChangedMessage());
        }

        public async Task<bool> SaveAsync(string filename, CancellationToken? token = null)
        {
            if (string.IsNullOrEmpty(filename))
                throw new InvalidOperationException("File path is empty.");

            using (LogManager.MeasureTime($"Save file : {filename}", TraceLevel.Debug))
            {
                _projectRepository.Save();
                await _writeService.WriteAsync(_projectRepository.GetCurrentOrThrow(), filename, token).ConfigureAwait(false);
            }

            Messenger.Default.Send(new CurrentProjectSavedMessage(_projectRepository.GetCurrentOrThrow(), filename));

            return true;
        }

        private static void Randomize(Project project)
        {
            using (ProgressManager.StartUncancellable(SubStepDefinitionsRandomize, MyClubResources.ProgressGeneratingRandomData))
            {
                // Tactics
                using (LogManager.MeasureTime("Create Tactics"))
                using (ProgressManager.Start())
                {
                    TacticRandomFactory.RandomKnownTactics().ForEach(x => project.AddTactic(x));
                    EnumerableHelper.Iteration(RandomGenerator.Int(1, 2), x => project.AddTactic(TacticRandomFactory.Random($"{MyClubResources.Tactic} {x + 1}")));
                }

                // Sended Mails
                using (LogManager.MeasureTime("Create Sended Mails"))
                    AddItemsWithProgress(SendedMailRandomFactory.RandomSendedMails(project.Players.SelectMany(x => x.Player.Emails).Select(x => x.Value).ToList(), project.Season.Period.Start).ToList(), x => project.AddSendedMail(x));

                // Periods
                using (LogManager.MeasureTime("Create Cycles and holidays"))
                {
                    var (cycles, holidays) = PeriodRandomFactory.RandomPeriods(project.Season.Period.Start, project.Season.Period.End);
                    AddItemsWithProgress(cycles.ToList(), x => project.AddCycle(x));
                    AddItemsWithProgress(holidays.ToList(), x => project.AddHolidays(x));
                }

                // Matches
                using (LogManager.MeasureTime("Randomize matches scores"))
                    AddItemsWithProgress(project.Competitions.SelectMany(x => x.GetAllMatches()).ToList(), x => x.RandomizeScore());

                // Cups
                using (LogManager.MeasureTime("Randomize cups"))
                    AddItemsWithProgress(project.Competitions.OfType<CupSeason>().ToList(), x =>
                    {
                        var qualifiedTeams = new List<Team>();

                        foreach (var round in x.Rounds)
                        {
                            var roundIsStarted = round.GetAllMatches().Any();

                            if (!roundIsStarted)
                            {
                                round.SetTeams(qualifiedTeams);

                                if (round is Knockout knockout)
                                    CompetitionExtensions.ComputeKnockoutMatches(knockout.Teams, knockout.Date, knockout.Rules.MatchFormat).ForEach(x => knockout.AddMatch(x));
                                else if (round is GroupStage groupStage)
                                    groupStage.ComputeRandomGroups();
                            }

                            round.GetAllMatches().ForEach(x => x.RandomizeScore());

                            var roundIsEnded = round.GetAllMatches().Any() && round.GetAllMatches().All(x => x.State == MatchState.Played);
                            if (roundIsEnded)
                            {
                                if (round is Knockout)
                                    qualifiedTeams = round.GetAllMatches().Select(x => x.GetWinner()).NotNull().ToList();
                                else if (round is GroupStage groupStage)
                                    qualifiedTeams = groupStage.Groups.SelectMany(x => x.GetRanking().Take(2).Select(y => y.Team)).ToList();
                            }

                            if (qualifiedTeams.Count == 0)
                                break;
                        }
                    });

                // Trainings
                using (LogManager.MeasureTime("Create Trainings"))
                    AddItemsWithProgress(TrainingSessionRandomFactory.RandomTrainingSessions(project.Season.Period.Start, project.Season.Period.End, project.Club.Teams, project.Players).ToList(), x => project.AddTrainingSession(x));
            }
        }

        private static void AddItemsWithProgress<T>(IReadOnlyCollection<T> collection, Action<T> addItem)
        {
            var count = collection.Count;

            using var progressStep = ProgressManager.Start();

            var index = 0;
            foreach (var item in collection)
            {
                addItem(item);
                progressStep?.UpdateProgress(++index / (double)count);
            }
        }
    }
}
