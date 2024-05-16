// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.Enums;
using MyClub.Scorer.Wpf.Services.Providers;
using MyNet.Humanizer;
using MyNet.Observable.Attributes;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Edition;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class ProjectEditionViewModel : EditionViewModel
    {
        private readonly ProjectService _projectService;
        private readonly ProjectInfoProvider _projectInfoProvider;

        [IsRequired]
        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        public string? Name { get; set; }

        public byte[]? Image { get; set; }

        public ProjectEditionGeneralViewModel GeneralViewModel { get; } = new();

        public ProjectEditionViewModel(ProjectInfoProvider projectInfoProvider, ProjectService projectService)
        {
            _projectService = projectService;
            _projectInfoProvider = projectInfoProvider;
            Mode = ScreenMode.Creation;

            AddSubWorkspaces(
            [
                GeneralViewModel,
            ]);

            Disposables.AddRange(
            [
                GeneralViewModel.WhenPropertyChanged(x => x.Type).Subscribe(_ =>
                {
                    var dictionary = Enum.GetValues<CompetitionType>().ToDictionary(x => x, x => x.Humanize());

                    if (string.IsNullOrEmpty(Name) || dictionary.ContainsValue(Name))
                        Name = dictionary.GetValueOrDefault(GeneralViewModel.Type);
                })
            ]);
        }

        protected override void RefreshCore()
        {
            if (Mode == ScreenMode.Edition)
            {
                Name = _projectInfoProvider.Name;
                Image = _projectInfoProvider.Image;
            }
            else
            {
                Name = GeneralViewModel.Type.Humanize();
                Image = null;
            }
        }

        protected override void SaveCore()
        {
            if (Mode != ScreenMode.Edition) return;

            _projectService.Update(new ProjectMetadataDto
            {
                Name = Name,
                Image = Image,
            });
        }

        protected override void Cleanup()
        {
            GeneralViewModel.Dispose();
            base.Cleanup();
        }
    }
}
