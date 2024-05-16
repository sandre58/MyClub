// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using MyNet.UI.Commands;
using MyNet.UI.Toasting;
using MyNet.UI.Toasting.Settings;
using MyNet.UI.ViewModels.Workspace;
using MyNet.Humanizer;
using MyNet.Observable.Attributes;
using MyNet.UI.Resources;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.Enums;

namespace MyClub.Scorer.Wpf.ViewModels.Shell
{
    internal class NewViewModel : WorkspaceViewModel
    {
        private readonly ProjectCommandsService _projectCommandsService;

        [IsRequired]
        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        public string? Name { get; set; }

        public byte[]? Image { get; set; }

        public CompetitionType Type { get; set; }

        public ICommand CreateCommand { get; }

        public NewViewModel(ProjectCommandsService projectCommandsService)
        {
            _projectCommandsService = projectCommandsService;
            CreateCommand = CommandsManager.Create(async () => await CreateAsync().ConfigureAwait(false));
            Name = Type.Humanize();
        }

        private async System.Threading.Tasks.Task CreateAsync()
        {
            if (ValidateProperties())
            {
                await _projectCommandsService.CreateAsync(Type, new ProjectMetadataDto
                {
                    Name = Name,
                    Image = Image
                }).ConfigureAwait(false);
            }
            else
            {
                GetErrors().ToList().ForEach(x => ToasterManager.ShowError(x, ToastClosingStrategy.AutoClose));
            }
        }

        protected override string CreateTitle() => UiResources.New;

        protected void OnTypeChanged()
        {
            var dictionary = Enum.GetValues<CompetitionType>().ToDictionary(x => x, x => x.Humanize());

            if (string.IsNullOrEmpty(Name) || dictionary.ContainsValue(Name))
                Name = dictionary.GetValueOrDefault(Type);
        }
    }
}
