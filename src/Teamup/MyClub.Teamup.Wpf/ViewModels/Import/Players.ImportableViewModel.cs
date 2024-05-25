// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using MyClub.CrossCutting.Localization;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.SquadAggregate;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.Observable.Attributes;
using MyNet.Observable.Translatables;
using MyNet.Observable.Validation;
using MyNet.UI.ViewModels.Import;
using MyNet.Utilities;
using MyNet.Utilities.Geography;
using MyNet.Utilities.Units;
using PropertyChanged;

namespace MyClub.Teamup.Wpf.ViewModels.Import
{
    internal class PlayerImportableViewModel : ImportableViewModel
    {
        public bool ImportInjuries { get; set; } = true;

        [Display(Name = nameof(LastName), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string LastName { get; }

        [Display(Name = nameof(FirstName), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string FirstName { get; }

        public TeamViewModel? Team { get; set; }

        public byte[]? Photo { get; set; }

        public GenderType Gender { get; set; }

        public Category? Category { get; set; }

        [Display(Name = nameof(Number), ResourceType = typeof(MyClubResources))]
        public AcceptableValue<int> Number { get; } = new AcceptableValue<int>(SquadPlayer.AcceptableRangeNumber);

        [Display(Name = nameof(MyClubResources.InClubFromDate), ResourceType = typeof(MyClubResources))]
        [IsInPast(true)]
        public DateTime? FromDate { get; set; }

        [Display(Name = nameof(Birthdate), ResourceType = typeof(MyClubResources))]
        [IsInPast(true)]
        public DateTime? Birthdate { get; set; }

        [Display(Name = nameof(PlaceOfBirth), ResourceType = typeof(MyClubResources))]
        public string? PlaceOfBirth { get; set; }

        [Display(Name = nameof(Country), ResourceType = typeof(MyClubResources))]
        public Country? Country { get; set; }

        [MaxLength(10)]
        [Display(Name = nameof(LicenseNumber), ResourceType = typeof(MyClubResources))]
        public string? LicenseNumber { get; set; }

        [Display(Name = nameof(LicenseState), ResourceType = typeof(MyClubResources))]
        public LicenseState LicenseState { get; set; }

        [Display(Name = nameof(IsMutation), ResourceType = typeof(MyClubResources))]
        public bool IsMutation { get; set; }

        [Display(Name = nameof(Description), ResourceType = typeof(MyClubResources))]
        public string? Description { get; set; }

        [Display(Name = nameof(Address), ResourceType = typeof(MyClubResources))]
        public string? Address { get; set; }

        [MaxLength(5)]
        [Display(Name = nameof(PostalCode), ResourceType = typeof(MyClubResources))]
        public string? PostalCode { get; set; }

        [Display(Name = nameof(City), ResourceType = typeof(MyClubResources))]
        public string? City { get; set; }

        [IsRequired]
        [Display(Name = nameof(Laterality), ResourceType = typeof(MyClubResources))]
        public Laterality Laterality { get; set; }

        [Display(Name = nameof(Height), ResourceType = typeof(MyClubResources))]
        public Length<int> Height { get; } = new Length<int>(Player.AcceptableRangeHeight, LengthUnit.Centimeter);

        [Display(Name = nameof(Weight), ResourceType = typeof(MyClubResources))]
        public Mass<int> Weight { get; } = new Mass<int>(Player.AcceptableRangeWeight, MassUnit.Kilogram);

        [Display(Name = nameof(ShoesSize), ResourceType = typeof(MyClubResources))]
        public AcceptableValue<int> ShoesSize { get; } = new AcceptableValue<int>(SquadPlayer.AcceptableRangeShoesSize);

        [MaxLength(4)]
        [Display(Name = nameof(Size), ResourceType = typeof(MyClubResources))]
        public string? Size { get; set; }

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public Phone? Phone => Phones.FirstOrDefault();

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public Email? Email => Emails.FirstOrDefault();

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public Position? Position => Positions.OrderByDescending(x => x.IsNatural).ThenByDescending(x => x.Rating).FirstOrDefault()?.Position;

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public ObservableCollection<Phone> Phones { get; } = [];

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public ObservableCollection<Email> Emails { get; } = [];

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public ObservableCollection<RatedPosition> Positions { get; } = [];

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public ObservableCollection<Injury> Injuries { get; } = [];

        [AlsoNotifyFor(nameof(Birthdate))]
        public int? Age => !Birthdate.HasValue ? null : Birthdate.Value.GetAge();

        public PlayerImportableViewModel(string lastName, string firstName, PlayerService playerService, ImportMode mode = ImportMode.Add, bool import = false) : base(mode, import)
        {
            LastName = lastName;
            FirstName = firstName;
            ValidationRules.Add<PlayerImportableViewModel, string?>(y => y.LicenseNumber, MyClubResources.LicenseNumberAlreadyExistsError, y => !playerService.LicenseNumberExists(y.OrEmpty()), ValidationRuleSeverity.Warning);
        }

        public Address? GetAddress()
            => !string.IsNullOrEmpty(Address) || !string.IsNullOrEmpty(PostalCode) || !string.IsNullOrEmpty(City)
                ? new Address(Address, PostalCode, City)
                : null;
    }
}
