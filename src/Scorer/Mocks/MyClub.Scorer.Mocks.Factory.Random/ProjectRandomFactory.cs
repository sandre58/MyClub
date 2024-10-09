// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyClub.CrossCutting.Images;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.BracketComputing;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.Factories;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.PersonAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.RankingAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using MyNet.Utilities.Generator;
using MyNet.Utilities.Generator.Extensions;
using MyNet.Utilities.Geography;
using MyNet.Utilities.Helpers;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Progress;
using MyNet.Utilities.Sequences;

namespace MyClub.Scorer.Mocks.Factory.Random
{
    public class ProjectRandomFactory(IProgresser progresser, ILogger logger) : IProjectFactory
    {
        private readonly IProgresser _progresser = progresser;
        private readonly ILogger _logger = logger;

        public async Task<LeagueProject> CreateLeagueAsync(CancellationToken cancellationToken = default)
        {
            var project = await CreateAsync((name, image, matchFormat, matchRules, schedulingParameters) =>
            {
                var competition = new LeagueProject(name, image);
                competition.Competition.SchedulingParameters = schedulingParameters;
                competition.Competition.MatchFormat = matchFormat;
                competition.Competition.MatchRules = matchRules;
                return competition;
            }, true, 8, 20, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            // Penaly points
            var teams = RandomGenerator.ListItems(project.Teams, RandomGenerator.Int(0, 3));
            teams.ForEach(x => project.Competition.AddPenalty(x, RandomGenerator.Int(1, 10)));

            // Labels
            project.Competition.Labels.Add(new AcceptableValueRange<int>(1, RandomGenerator.Int(1, 3)), new RankLabel(RandomGenerator.Color(), MyClubResources.Promotion, MyClubResources.PromotionAbbr));
            project.Competition.Labels.Add(new AcceptableValueRange<int>(project.Teams.Count - RandomGenerator.Int(2, 4), project.Teams.Count), new RankLabel(RandomGenerator.Color(), MyClubResources.Relegation, MyClubResources.RelegationAbbr));

            // Scheduler
            var matchdaysScheduler = project.Competition.SchedulingParameters.AsSoonAsPossible
                ? new AsSoonAsPossibleStageScheduler<Matchday>()
                {
                    StartDate = project.Competition.SchedulingParameters.Start(),
                    Rules = [.. project.Competition.SchedulingParameters.AsSoonAsPossibleRules],
                    ScheduleVenues = true,
                    AvailableStadiums = project.Stadiums
                }
                : (IScheduler<Matchday>)new DateRulesStageScheduler<Matchday>()
                {
                    Interval = project.Competition.SchedulingParameters.Interval,
                    DateRules = [.. project.Competition.SchedulingParameters.DateRules],
                    TimeRules = [.. project.Competition.SchedulingParameters.TimeRules],
                    DefaultTime = project.Competition.SchedulingParameters.StartTime,
                    StartDate = project.Competition.SchedulingParameters.StartDate
                };
            var venueScheduler = project.Competition.SchedulingParameters.UseHomeVenue ? (IMatchesScheduler)new HomeTeamVenueMatchesScheduler() : project.Competition.SchedulingParameters.AsSoonAsPossible ? null : new VenueRulesMatchesScheduler(project.Stadiums)
            {
                Rules = [.. project.Competition.SchedulingParameters.VenueRules],
            };

            // Algorithm
            var numberOfMatchesBetweenTeams = RandomGenerator.Int(1, 2);
            var algorithms = new List<IMatchdaysAlgorithm>
            {
                new RoundRobinAlgorithm() { NumberOfMatchesBetweenTeams = numberOfMatchesBetweenTeams, InvertTeamsByStage = numberOfMatchesBetweenTeams.Range().Select(x => x.IsEven()).ToArray() }
            };
            if (project.Teams.Count.IsEven())
                algorithms.Add(new SwissSystemAlgorithm() { NumberOfMatchesByTeams = RandomGenerator.Int(project.Teams.Count / 3, project.Teams.Count - 1) });

            var matchdays = new MatchdaysBuilder(matchdaysScheduler, venueScheduler).Build(project.Competition, RandomGenerator.ListItem(algorithms));

            matchdays.ForEach(x =>
            {
                x.Matches.ForEach(match =>
                {
                    if (match.Date.IsInPast())
                        match.Randomize(!match.GetPeriod().Contains(DateTime.UtcNow));
                    match.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);
                });
                x.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

                project.Competition.AddMatchday(x);
            });

            return project;
        }

        public async Task<CupProject> CreateCupAsync(CancellationToken cancellationToken = default)
        {
            var project = await CreateAsync((name, image, matchFormat, matchRules, schedulingParameters) =>
            {
                var competition = new CupProject(name, image);
                competition.Competition.SchedulingParameters = schedulingParameters;
                competition.Competition.MatchFormat = matchFormat;
                competition.Competition.MatchRules = matchRules;
                return competition;
            }, false, 16, 128, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            // Scheduler
            var roundsScheduler = project.Competition.SchedulingParameters.AsSoonAsPossible
                ? new AsSoonAsPossibleStageScheduler<RoundOfMatches>()
                {
                    StartDate = project.Competition.SchedulingParameters.Start(),
                    Rules = [.. project.Competition.SchedulingParameters.AsSoonAsPossibleRules],
                    ScheduleVenues = true,
                    AvailableStadiums = project.Stadiums
                }
                : (IScheduler<RoundOfMatches>)new DateRulesStageScheduler<RoundOfMatches>()
                {
                    Interval = project.Competition.SchedulingParameters.Interval,
                    DateRules = [.. project.Competition.SchedulingParameters.DateRules],
                    TimeRules = [.. project.Competition.SchedulingParameters.TimeRules],
                    DefaultTime = project.Competition.SchedulingParameters.StartTime,
                    StartDate = project.Competition.SchedulingParameters.StartDate
                };
            var venueScheduler = project.Competition.SchedulingParameters.UseHomeVenue ? (IMatchesScheduler)new HomeTeamVenueMatchesScheduler() : project.Competition.SchedulingParameters.AsSoonAsPossible ? null : new VenueRulesMatchesScheduler(project.Stadiums)
            {
                Rules = [.. project.Competition.SchedulingParameters.VenueRules],
            };

            // Algorithm
            var numberOfMatchesBetweenTeams = RandomGenerator.Int(1, 2);
            var algorithms = new List<IRoundsAlgorithm>
            {
                new KnockoutAlgorithm()
            };

            var rounds = new RoundOfMatchesBuilder(roundsScheduler, venueScheduler).Build(project.Competition, RandomGenerator.ListItem(algorithms));

            rounds.ForEach(x =>
            {
                x.GetAllMatches().ForEach(match =>
                {
                    match.ComputeOpponents();
                    if (match.Date.IsInPast())
                        match.Randomize(!match.GetPeriod().Contains(DateTime.UtcNow));
                    match.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);
                });
                x.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

                project.Competition.AddRound(x);
            });
            return project;
        }

        public async Task<TournamentProject> CreateTournamentAsync(CancellationToken cancellationToken = default)
            => await CreateAsync((name, image, matchFormat, matchRules, schedulingParameters) =>
            {
                var competition = new TournamentProject(name, image);
                competition.Competition.SchedulingParameters = schedulingParameters;
                competition.Competition.MatchFormat = matchFormat;
                competition.Competition.MatchRules = matchRules;
                return competition;
            }, false, 32, 128, cancellationToken).ConfigureAwait(false);

        public Task<T> CreateAsync<T>(Func<string, byte[]?, MatchFormat, MatchRules, SchedulingParameters, T> createInstance, bool allowDraw, int minTeams, int maxTeams, CancellationToken cancellationToken = default)
            where T : IProject
        {
            using (_progresser.Start(3, new ProgressMessage(string.Empty)))
            {
                T? project;
                var asSoonAsPossible = RandomGenerator.Bool();

                using (_progresser.Start(new ProgressMessage(string.Empty)))
                {
                    var name = $"{NameGenerator.LastName()} Cup";
                    project = createInstance(name, RandomProvider.GetCompetitionLogo(), CreateMatchFormat(asSoonAsPossible, allowDraw), CreateMatchRules(), CreateScheduleParameters(asSoonAsPossible));
                }

                if (project is null) throw new InvalidOperationException($"Impossible to create an instance of {typeof(T)}");

                _logger.Trace($"Creation of project {project.Name}");

                // Preferences
                project.Preferences.TreatNoStadiumAsWarning = RandomGenerator.Bool();
                project.Preferences.PeriodForPreviousMatches = asSoonAsPossible ? RandomGenerator.Int(1, 2).Hours() : RandomGenerator.Int(5, 8).Days();
                project.Preferences.PeriodForNextMatches = asSoonAsPossible ? RandomGenerator.Int(1, 2).Hours() : RandomGenerator.Int(5, 8).Days();

                cancellationToken.ThrowIfCancellationRequested();

                // Teams
                using (_progresser.Start(new ProgressMessage(string.Empty)))
                {
                    var teams = EnumerableHelper.Range(1, RandomGenerator.Int(minTeams, maxTeams), 1).Select(_ => AddressGenerator.City()).Distinct().Select(x =>
                    {
                        var team = new Team(x)
                        {
                            AwayColor = RandomGenerator.Color(),
                            HomeColor = RandomGenerator.Color(),
                            Stadium = CreateStadium(),
                            Country = RandomGenerator.ListItem(Enumeration.GetAll<Country>().ToList()),
                            Logo = RandomProvider.GetTeamLogo()
                        };
                        team.AddManagers(RandomGenerator.Int(1, 2).Range().Select(y =>
                        {
                            var manager = new Manager(team, NameGenerator.FirstName().ToSentence(), NameGenerator.LastName().ToSentence())
                            {
                                Country = RandomGenerator.Country(),
                                Gender = GenderType.Male,
                                Email = InternetGenerator.Email(),
                                Photo = RandomProvider.GetPhoto(GenderType.Male),
                                LicenseNumber = string.Join("", RandomGenerator.Digits(10)),
                            };
                            manager.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

                            return manager;
                        }).ToList());
                        team.AddPlayers(RandomGenerator.Int(10, 20).Range().Select(y =>
                        {
                            var player = new Player(team, NameGenerator.FirstName().ToSentence(), NameGenerator.LastName().ToSentence())
                            {
                                Country = RandomGenerator.Country(),
                                Gender = GenderType.Male,
                                Email = InternetGenerator.Email(),
                                Photo = RandomProvider.GetPhoto(GenderType.Male),
                                LicenseNumber = string.Join("", RandomGenerator.Digits(10)),
                            };
                            player.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

                            return player;
                        }).ToList());
                        team.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

                        return team;
                    }).ToList();

                    teams.ForEach(x =>
                    {
                        project.AddTeam(x);

                        if (x.Stadium is not null)
                        {
                            x.Stadium.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);
                            project.AddStadium(x.Stadium);
                        }
                    });
                }

                // Stadiums
                using (_progresser.Start(new ProgressMessage(string.Empty)))
                {
                    if (!project.Competition.SchedulingParameters.UseHomeVenue)
                    {
                        var countStadiums = project.Competition.SchedulingParameters.AsSoonAsPossible ? RandomGenerator.Int(2, project.Teams.Count / 2) : RandomGenerator.Int(project.Teams.Count / 2, project.Teams.Count);
                        countStadiums.Range().Select(x => CreateStadium()).ForEach(x => project.AddStadium(x));
                    }
                }

                project.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

                return Task.FromResult(project);
            }
        }

        private static MatchRules CreateMatchRules() => MatchRules.Default;

        private static MatchFormat CreateMatchFormat(bool asSoonAsPossible, bool allowDraw) => asSoonAsPossible
                ? new MatchFormat(new HalfFormat(RandomGenerator.Int(1, 2), RandomGenerator.Int(8, 30).Minutes(), 5.Minutes()), allowDraw ? null : new HalfFormat(RandomGenerator.Int(1, 2), RandomGenerator.Int(4, 10).Minutes(), 2.Minutes()), allowDraw ? null : RandomGenerator.Int(3, 5))
                : (allowDraw ? MatchFormat.Default : MatchFormat.NoDraw);

        private static SchedulingParameters CreateScheduleParameters(bool asSoonAsPossible)
        {
            var useHomeVenue = RandomGenerator.Bool();
            return new SchedulingParameters(asSoonAsPossible ? DateTime.UtcNow.AddDays(-RandomGenerator.Int(1, 2)).ToDate() : DateTime.UtcNow.AddMonths(-RandomGenerator.Int(2, 4)).ToDate(),
                                            asSoonAsPossible ? DateTime.UtcNow.AddDays(RandomGenerator.Int(3, 6)).ToDate() : DateTime.UtcNow.AddMonths(RandomGenerator.Int(4, 6)).ToDate(),
                                            RandomGenerator.Int(8, 22).Hours().ToTime(),
                                            asSoonAsPossible ? RandomGenerator.Int(2, 5).Minutes() : RandomGenerator.Int(1, 3).Days(),
                                            asSoonAsPossible ? RandomGenerator.Int(5, 30).Minutes() : RandomGenerator.Int(1, 6).Days(),
                                            useHomeVenue,
                                            asSoonAsPossible,
                                            1.Days(),
                                            true,
                                            asSoonAsPossible ? [new IncludeTimePeriodsRule([new TimePeriod(RandomGenerator.Int(9, 14).Hours().ToTime(), RandomGenerator.Int(16, 20).Hours().ToTime())])] : [],
                                            !asSoonAsPossible ? [new IncludeDaysOfWeekRule(RandomGenerator.ListItems(Enum.GetValues<DayOfWeek>(), 1))] : [],
                                            [],
                                            !useHomeVenue && !asSoonAsPossible ? [new FirstAvailableStadiumRule(UseRotationTime.YesOrOtherwiseNo)] : []);
        }

        private static Stadium CreateStadium()
        {
            var ground = RandomGenerator.Enum<Ground>();

            var item = new Stadium(MyClubResources.Stadium + $" {NameGenerator.LastName()}", ground)
            {
                Address = new Address(
                    AddressGenerator.Street(),
                    AddressGenerator.PostalCode(),
                    AddressGenerator.City().ToSentence(),
                    RandomGenerator.Country(),
                    AddressGenerator.Coordinates().Latitude,
                    AddressGenerator.Coordinates().Longitude),
            };
            item.MarkedAsCreated(DateTime.UtcNow, MyClubResources.System);

            return item;
        }
    }
}
