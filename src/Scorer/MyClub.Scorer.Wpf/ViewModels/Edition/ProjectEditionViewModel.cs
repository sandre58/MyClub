// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Observable.Attributes;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Edition;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class ProjectEditionViewModel : EditionViewModel
    {
        private readonly ProjectService _projectService;
        private readonly ProjectInfoProvider _projectInfoProvider;
        private readonly LeagueService _leagueService;
        private readonly CompetitionInfoProvider _competitionInfoProvider;

        public ProjectEditionViewModel(ProjectInfoProvider projectInfoProvider, CompetitionInfoProvider competitionInfoProvider, StadiumsProvider stadiumsProvider, ProjectService projectService, LeagueService leagueService)
        {
            _projectService = projectService;
            _projectInfoProvider = projectInfoProvider;
            _competitionInfoProvider = competitionInfoProvider;
            _leagueService = leagueService;
            SchedulingParameters = new(stadiumsProvider.Items);
            Mode = ScreenMode.Edition;
        }

        [IsRequired]
        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        public string? Name { get; set; }

        public byte[]? Image { get; set; }

        public EditableMatchFormatViewModel MatchFormat { get; } = new();

        public EditableSchedulingParametersViewModel SchedulingParameters { get; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public bool CanEditMatchFormat { get; private set; }

        protected override void RefreshCore()
        {
            Name = _projectInfoProvider.Name;
            Image = _projectInfoProvider.Image;

            var competition = _competitionInfoProvider.GetCompetition();

            CanEditMatchFormat = !_competitionInfoProvider.HasMatches;
            MatchFormat.Load(competition.MatchFormat);
            //SchedulingParameters.Load(competition.SchedulingParameters);
        }

        protected override void SaveCore()
        {
            if (Mode != ScreenMode.Edition) return;

            _projectService.Update(new ProjectMetadataDto
            {
                Name = Name,
                Image = Image,
            });

            var competition = _competitionInfoProvider.GetCompetition();

            if (competition is LeagueViewModel)
            {
                if (CanEditMatchFormat)
                    _leagueService.UpdateMatchFormat(MatchFormat.Create());
                _leagueService.UpdateSchedulingParameters(SchedulingParameters.Create());
            }
        }

        protected override void Cleanup()
        {
            MatchFormat.Dispose();
            base.Cleanup();
        }
    }
}
