// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Contracts;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Progress;

namespace MyClub.Scorer.Application.Services
{
    public sealed class ProjectService(IProjectRepository projectRepository,
                                       IReadService readService,
                                       IWriteService writeService,
                                       IProjectFactory projectFactory)
    {
        private readonly IProjectRepository _projectRepository = projectRepository;
        private readonly IProjectFactory _projectFactory = projectFactory;
        private readonly IReadService _readService = readService;
        private readonly IWriteService _writeService = writeService;

        public async Task<IProject> NewAsync(CompetitionType type, CancellationToken cancellationToken = default)
        {
            using (ProgressManager.Start([0.5, 0.5]))
            {
                IProject project;

                using (LogManager.MeasureTime($"Create default project", TraceLevel.Debug))
                using (ProgressManager.Start(MyClubResources.ProgressGeneratingProject))
                    project = await NewProjectAsync(type, cancellationToken).ConfigureAwait(false);

                _ = Load(project);

                return project;
            }
        }

        private async Task<IProject> NewProjectAsync(CompetitionType type, CancellationToken cancellationToken)
            => type switch
            {
                CompetitionType.League => await _projectFactory.CreateLeagueAsync(cancellationToken).ConfigureAwait(false),
                CompetitionType.Cup => await _projectFactory.CreateCupAsync(cancellationToken).ConfigureAwait(false),
                CompetitionType.Tournament => await _projectFactory.CreateTournamentAsync(cancellationToken).ConfigureAwait(false),
                _ => throw new InvalidOperationException(),
            };

        public async Task<LeagueProject> CreateLeagueAsync(LeagueMetadataDto dto)
            => await CreateAsync<LeagueProject, League>(dto, () => new LeagueProject(dto.Name.OrEmpty(), dto.Image), x =>
            {
                if (dto.RankingRules?.Rules is not null)
                    x.RankingRules = dto.RankingRules.Rules;

                if (dto.RankingRules?.PenaltyPoints is not null)
                    dto.RankingRules.PenaltyPoints.ForEach(z => x.AddPenalty(z.Key, z.Value));

                if (dto.RankingRules?.Labels is not null)
                    x.Labels.AddRange(dto.RankingRules.Labels);
            }, (x, y, z) =>
            {
                if (z is BuildMatchdaysParametersDto matchdaysParametersDto)
                {
                    var matchdays = LeagueService.ComputeMatchdays(x, matchdaysParametersDto, y);
                    matchdays.ForEach(z => x.AddMatchday(z));
                }
            }).ConfigureAwait(false);

        public async Task<CupProject> CreateCupAsync(CupMetadataDto dto)
            => await CreateAsync<CupProject, Cup>(dto, () => new CupProject(dto.Name.OrEmpty(), dto.Image), x =>
            {
            }, (x, y, z) => { }).ConfigureAwait(false);

        public async Task<TournamentProject> CreateTournamentAsync(TournamentMetadataDto dto)
            => await CreateAsync<TournamentProject, Tournament>(dto, () => new TournamentProject(dto.Name.OrEmpty(), dto.Image), x =>
            {
            }, (x, y, z) => { }).ConfigureAwait(false);

        private Task<T> CreateAsync<T, TCompetition>(ProjectMetadataDto dto, Func<T> createProject, Action<TCompetition> updateCompetition, Action<TCompetition, IList<Stadium>, BuildBracketParametersDto> buildCompetition)
            where T : Project<TCompetition>
            where TCompetition : ICompetition, new()
        {
            T project;

            using (ProgressManager.Start([0.2, 0.2, 0.01, 0.3, 0.09, 0.05, 0.15]))
            {
                using (LogManager.MeasureTime($"Create new project : {dto.Name}", TraceLevel.Debug))
                {
                    project = createProject();
                    if (dto.Preferences is not null)
                        UpdatePreferences(project, dto.Preferences);

                    // Create stadiums
                    using (ProgressManager.Start())
                    {
                        dto.Stadiums?.ForEach(x => project.AddStadium(new Stadium(x.Name.OrEmpty(), x.Ground, x.Id) { Address = x.Address }));
                    }

                    // Create teams
                    using (ProgressManager.Start())
                    {
                        dto.Teams?.ForEach(x => project.AddTeam(new Team(x.Name.OrEmpty(), x.ShortName.OrEmpty(), x.Id)
                        {
                            AwayColor = x.AwayColor,
                            Country = x.Country,
                            HomeColor = x.HomeColor,
                            Logo = x.Logo,
                            Stadium = x.Stadium?.Id is not null ? project.Stadiums.GetById(x.Stadium.Id.Value) : null
                        }));
                    }

                    // Update Rules & Format
                    using (ProgressManager.Start())
                    {
                        if (dto.MatchRules is not null)
                            project.Competition.MatchRules = dto.MatchRules;

                        if (dto.BuildParameters?.MatchFormat is not null)
                            project.Competition.MatchFormat = dto.BuildParameters.MatchFormat;
                    }

                    // Build competition
                    using (ProgressManager.Start())
                    {
                        if (dto.BuildParameters?.BracketParameters is BuildMatchdaysParametersDto buildMatchdaysParametersDto)
                            buildCompetition(project.Competition, project.Stadiums, dto.BuildParameters.BracketParameters);
                    }

                    // Schedule competition
                    using (ProgressManager.Start())
                    {
                        if (dto.BuildParameters?.SchedulingParameters is not null)
                        {
                            var allSchedulables = project.Competition.GetAllSchedulables().ToList();
                            project.Competition.SchedulingParameters = new SchedulingParameters(
                               dto.BuildParameters.AutomaticStartDate ? allSchedulables.MinOrDefault(x => x.Date.ToDate()) : dto.BuildParameters.SchedulingParameters.StartDate,
                               dto.BuildParameters.AutomaticEndDate ? allSchedulables.MaxOrDefault(x => x.Date.ToDate()) : dto.BuildParameters.SchedulingParameters.EndDate,
                               dto.BuildParameters.SchedulingParameters.StartTime,
                               dto.BuildParameters.SchedulingParameters.RotationTime,
                               dto.BuildParameters.SchedulingParameters.RestTime,
                               dto.BuildParameters.SchedulingParameters.UseHomeVenue,
                               dto.BuildParameters.SchedulingParameters.AsSoonAsPossible,
                               dto.BuildParameters.SchedulingParameters.Interval,
                               dto.BuildParameters.SchedulingParameters.ScheduleByStage,
                               dto.BuildParameters.SchedulingParameters.AsSoonAsPossibleRules,
                               dto.BuildParameters.SchedulingParameters.DateRules,
                               dto.BuildParameters.SchedulingParameters.TimeRules,
                               dto.BuildParameters.SchedulingParameters.VenueRules
                               );
                        }
                    }

                    using (ProgressManager.Start())
                    {
                        updateCompetition(project.Competition);
                    }

                    _ = Load(project);
                }

                return Task.FromResult(project);
            }
        }

        public void Update(ProjectMetadataDto dto)
            => _projectRepository.Update(x =>
            {
                x.Name = dto.Name.OrThrow();
                x.Image = dto.Image;

                if (dto.Preferences is not null)
                    UpdatePreferences(x, dto.Preferences);
            });

        public IProject Load(IProject project)
        {
            LoadProject(project);

            return project;
        }

        public async Task<IProject> LoadAsync(string filename, CancellationToken? token = null)
        {
            if (string.IsNullOrEmpty(filename))
                throw new InvalidOperationException("File path is empty.");

            IProject project;
            using (LogManager.MeasureTime($"Read file : {filename}", TraceLevel.Debug))
            using (ProgressManager.Start([0.8, 0.2], nameof(MyClubResources.ProgressReadingFile), filename))
                project = await _readService.ReadAsync(filename, token).ConfigureAwait(false);

            LoadProject(project);

            return project;
        }

        private void LoadProject(IProject project)
        {
            using (LogManager.MeasureTime($"Load project : {project.Name}", TraceLevel.Trace))
            using (ProgressManager.StartUncancellable(MyClubResources.ProgressLoadingProject))
                _projectRepository.Load(project);
        }

        public void Close() => _projectRepository.Clear();

        public async Task<bool> SaveAsync(string filename, CancellationToken? token = null)
        {
            if (string.IsNullOrEmpty(filename))
                throw new InvalidOperationException("File path is empty.");

            using (LogManager.MeasureTime($"Save file : {filename}", TraceLevel.Debug))
            {
                _projectRepository.Save();
                await _writeService.WriteAsync(_projectRepository.GetCurrentOrThrow(), filename, token).ConfigureAwait(false);
            }

            return true;
        }

        private static void UpdatePreferences(IProject project, PreferencesDto dto)
        {
            project.Preferences.TreatNoStadiumAsWarning = dto.TreatNoStadiumAsWarning;
            project.Preferences.PeriodForPreviousMatches = dto.PeriodForPreviousMatches;
            project.Preferences.PeriodForNextMatches = dto.PeriodForNextMatches;
            project.Preferences.ShowNextMatchFallback = dto.ShowNextMatchFallback;
            project.Preferences.ShowLastMatchFallback = dto.ShowLastMatchFallback;
        }
    }
}
