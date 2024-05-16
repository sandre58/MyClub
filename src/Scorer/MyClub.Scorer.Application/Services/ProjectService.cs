// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Contracts;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Progress;

namespace MyClub.Scorer.Application.Services
{
    public sealed class ProjectService(
        IProjectRepository projectRepository,
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
                    project = await CreateProjectAsync(type, cancellationToken).ConfigureAwait(false);

                _ = Load(project);

                return project;
            }
        }

        public LeagueProject NewLeague(ProjectMetadataDto properties)
        {
            LeagueProject project;

            using (ProgressManager.Start([1]))
            {
                using (LogManager.MeasureTime($"Create new project : {properties.Name}", TraceLevel.Debug))
                {
                    project = new LeagueProject(properties.Name.OrThrow(), properties.StartDate, properties.EndDate, properties.Image);

                    _ = Load(project);
                }

                return project;
            }
        }

        public CupProject NewCup(ProjectMetadataDto properties)
        {
            CupProject project;

            using (ProgressManager.Start([1]))
            {
                using (LogManager.MeasureTime($"Create new project : {properties.Name}", TraceLevel.Debug))
                {
                    project = new CupProject(properties.Name.OrThrow(), properties.StartDate, properties.EndDate, properties.Image);

                    _ = Load(project);
                }

                return project;
            }
        }

        public TournamentProject NewTournament(ProjectMetadataDto properties)
        {
            TournamentProject project;

            using (ProgressManager.Start([1]))
            {
                using (LogManager.MeasureTime($"Create new project : {properties.Name}", TraceLevel.Debug))
                {
                    project = new TournamentProject(properties.Name.OrThrow(), properties.StartDate, properties.EndDate, properties.Image);

                    _ = Load(project);
                }

                return project;
            }
        }

        public void Update(ProjectMetadataDto properties)
            => _projectRepository.Update(x =>
            {
                x.Name = properties.Name.OrThrow();
                x.Image = properties.Image;
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

        private async Task<IProject> CreateProjectAsync(CompetitionType type, CancellationToken cancellationToken)
            => type switch
            {
                CompetitionType.League => await _projectFactory.CreateLeagueAsync(cancellationToken).ConfigureAwait(false),
                CompetitionType.Cup => await _projectFactory.CreateCupAsync(cancellationToken).ConfigureAwait(false),
                CompetitionType.Tournament => await _projectFactory.CreateTournamentAsync(cancellationToken).ConfigureAwait(false),
                _ => throw new InvalidOperationException(),
            };
    }
}
