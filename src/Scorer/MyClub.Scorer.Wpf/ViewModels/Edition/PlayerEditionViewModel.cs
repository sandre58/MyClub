// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.PersonAggregate;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Observable.Attributes;
using MyNet.Utilities;
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class PlayerEditionViewModel : EntityEditionViewModel<Player, PlayerDto, PlayerService>
    {
        [Display(Name = nameof(LastName), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = nameof(FirstName), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string FirstName { get; set; } = string.Empty;

        public TeamViewModel? Team { get; private set; }

        public byte[]? Photo { get; set; }

        public GenderType Gender { get; set; }

        [Display(Name = nameof(Country), ResourceType = typeof(MyClubResources))]
        public Country? Country { get; set; }

        [MaxLength(10)]
        [Display(Name = nameof(LicenseNumber), ResourceType = typeof(MyClubResources))]
        public string? LicenseNumber { get; set; }

        public PlayerEditionViewModel(PlayerService playerService)
            : base(playerService)
        {
        }

        public void Load(TeamViewModel team, Guid id)
        {
            Team = team;
            Load(id);
        }

        public void New(TeamViewModel team, Action? initialize = null)
        {
            Team = team;
            New(initialize);
        }

        protected override PlayerDto ToDto() => new()
        {
            Id = ItemId,
            FirstName = FirstName,
            LastName = LastName,
            TeamId = Team?.Id ?? Guid.Empty,
            Photo = Photo,
            Gender = Gender,
            LicenseNumber = LicenseNumber,
            Country = Country,
        };

        protected override void RefreshFrom(Player item)
        {
            FirstName = item.FirstName;
            LastName = item.LastName;
            Photo = item.Photo;
            Gender = item.Gender;
            LicenseNumber = item.LicenseNumber;
            Country = item.Country;
        }

        protected override void ResetItem()
        {
            var defaultValues = CrudService.New(Team?.Id ?? Guid.Empty);
            FirstName = defaultValues.FirstName.OrEmpty();
            LastName = defaultValues.LastName.OrEmpty();
            Photo = defaultValues.Photo;
            LicenseNumber = defaultValues.LicenseNumber;
            Gender = defaultValues.Gender;
            Country = defaultValues.Country;
        }
    }
}
