// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.TeamAggregate;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyNet.Observable.Attributes;
using MyNet.Utilities;
using MyNet.Utilities.Geography;
using MyNet.Wpf.Extensions;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class TeamEditionViewModel : EntityEditionViewModel<Team, TeamDto, TeamService>, IEntityEditionViewModel
    {
        private IEnumerable<string>? _existingTeamNames;

        public TeamEditionViewModel(TeamService service,
                                    StadiumsProvider stadiumsProvider,
                                    StadiumPresentationService stadiumPresentationService) : base(service)
        {
            StadiumSelectionViewModel = new(stadiumsProvider, stadiumPresentationService);
            ValidationRules.AddNotNull<TeamEditionViewModel, string>(x => x.Name, MyClubResources.DuplicatedTeamNameError, x => _existingTeamNames is null || !_existingTeamNames.Contains(x));

            Disposables.AddRange(
            [
                StadiumSelectionViewModel.WhenAnyPropertyChanged(nameof(StadiumSelectionViewModel.SelectedItem), nameof(StadiumSelectionViewModel.TextSearch)).Subscribe(_ => RaisePropertyChanged(nameof(NewStadiumWillBeCreated)))
            ]);
        }

        [Display(Name = nameof(Name), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string Name { get; set; } = string.Empty;

        [Display(Name = nameof(ShortName), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string ShortName { get; set; } = string.Empty;

        public byte[]? Logo { get; set; }

        public Color? HomeColor { get; set; }

        public Color? AwayColor { get; set; }

        public Country? Country { get; set; }

        public StadiumSelectionViewModel StadiumSelectionViewModel { get; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool NewStadiumWillBeCreated => StadiumSelectionViewModel.SelectedItem is null && !string.IsNullOrEmpty(StadiumSelectionViewModel.TextSearch);

        protected override TeamDto ToDto() => new()
        {
            Id = ItemId,
            Name = Name,
            ShortName = ShortName,
            Logo = Logo,
            AwayColor = AwayColor?.ToHex(),
            HomeColor = HomeColor?.ToHex(),
            Country = Country,
            Stadium = StadiumSelectionViewModel.SelectedItem is not null
                      ? new StadiumDto
                      {
                          Id = StadiumSelectionViewModel.SelectedItem.Id
                      }
                      : !string.IsNullOrEmpty(StadiumSelectionViewModel.TextSearch)
                      ? new StadiumDto
                      {
                          Name = StadiumSelectionViewModel.TextSearch,
                      }
                      : null

        };

        protected override void RefreshFrom(Team item)
        {
            _existingTeamNames = CrudService.GetAll().Except([item]).Select(x => x.Name).ToList();
            Name = item.Name.OrEmpty();
            ShortName = item.ShortName.OrEmpty();
            Logo = item.Logo;
            AwayColor = item.AwayColor?.ToColor();
            HomeColor = item.HomeColor?.ToColor();
            Country = item.Country;
            StadiumSelectionViewModel.Select(item.Stadium?.Id);
        }

        protected override void ResetItem()
        {
            var defaultValues = CrudService.NewTeam();
            Name = defaultValues.Name.OrEmpty();
            ShortName = defaultValues.ShortName.OrEmpty();
            Logo = defaultValues.Logo;
            AwayColor = defaultValues.AwayColor?.ToColor();
            HomeColor = defaultValues.HomeColor?.ToColor();
            Country = defaultValues.Country;
            StadiumSelectionViewModel.Select(defaultValues.Stadium?.Id);
        }
    }
}
