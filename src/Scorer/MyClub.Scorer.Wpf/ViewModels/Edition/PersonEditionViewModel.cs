// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using MyClub.Application.Services;
using MyClub.CrossCutting.Localization;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.PersonAggregate;
using MyNet.Observable.Attributes;
using MyNet.Utilities;
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal abstract class PersonEditionViewModel<TPerson, TPersonDto, TCrudService> : EntityEditionViewModel<TPerson, TPersonDto, TCrudService>
        where TCrudService : ICrudService<TPerson, TPersonDto>
        where TPerson : Person
        where TPersonDto : PersonDto, new()
    {
        public PersonEditionViewModel(TCrudService service) : base(service)
        {
        }

        [Display(Name = nameof(LastName), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = nameof(FirstName), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string FirstName { get; set; } = string.Empty;

        public byte[]? Photo { get; set; }

        public GenderType Gender { get; set; }

        [Display(Name = nameof(Country), ResourceType = typeof(MyClubResources))]
        public Country? Country { get; set; }

        [MaxLength(10)]
        [Display(Name = nameof(LicenseNumber), ResourceType = typeof(MyClubResources))]
        public string? LicenseNumber { get; set; }

        protected override TPersonDto ToDto() => new()
        {
            Id = ItemId,
            FirstName = FirstName,
            LastName = LastName,
            Photo = Photo,
            Gender = Gender,
            LicenseNumber = LicenseNumber,
            Country = Country,
        };

        protected override void RefreshFrom(TPerson item)
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
            FirstName = string.Empty;
            LastName = string.Empty;
            Photo = null;
            LicenseNumber = string.Empty;
            Gender = GenderType.Male;
            Country = null;
        }
    }
}
