// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Domain.PersonAggregate;
using MyClub.Teamup.Domain.SquadAggregate;
using MyNet.Observable.Attributes;
using MyNet.Observable.Translatables;
using MyNet.UI.Commands;
using MyNet.Utilities;
using MyNet.Utilities.Geography;
using MyNet.Utilities.Units;
using PropertyChanged;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class PlayerEditionViewModel : EntityEditionViewModel<SquadPlayer, SquadPlayerDto, PlayerService>
    {
        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        [UpdateOnCultureChanged]
        public virtual IList<string> Sizes => MyClubResources.SizesList.Split(';');

        public IReadOnlyCollection<EditableRatedPositionViewModel>? AllPositions { get; private set; }

        [DoNotCheckEquality]
        public IEnumerable? SelectedPositions { get; set; }

        public ICommand AddPhoneCommand { get; }

        public ICommand RemovePhoneCommand { get; }

        public ICommand AddEmailCommand { get; }

        public ICommand RemoveEmailCommand { get; }

        public ICommand UpSizeCommand { get; }

        public ICommand DownSizeCommand { get; }

        public ICommand RemoveAllPositionsCommand { get; }

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        public bool AutoUpdate { get; set; } = true;

        [Display(Name = nameof(LastName), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = nameof(FirstName), ResourceType = typeof(MyClubResources))]
        [IsRequired]
        public string FirstName { get; set; } = string.Empty;

        public Guid? TeamId { get; set; }

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

        public EditableAddressViewModel Address { get; }

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

        public ObservableCollection<EditablePhoneViewModel> Phones { get; } = [];

        public ObservableCollection<EditableEmailViewModel> Emails { get; } = [];

        public PlayerEditionViewModel(
            PlayerService playerService,
            AddressService addressService)
            : base(playerService)
        {
            Address = new(addressService);

            Disposables.AddRange(
            [
                this.WhenPropertyChanged(x => x!.Birthdate).Subscribe(x =>
                {
                    if (x.Value.HasValue && x.Sender.Category is null && !IsModifiedSuspender.IsSuspended)
                    {
                        x.Sender.Category = Category.FromBirthdate(x.Value.Value);
                    }
                }),
            ]);

            DownSizeCommand = CommandsManager.Create<object>(DownSize, CanDownSize);
            UpSizeCommand = CommandsManager.Create<object>(UpSize, CanUpSize);
            AddEmailCommand = CommandsManager.CreateNotNull<EditableEmailViewModel>(_ => AddNewEmail(), CanAddEmail);
            RemoveEmailCommand = CommandsManager.CreateNotNull<EditableEmailViewModel>(RemoveEmail, CanRemoveEmail);
            AddPhoneCommand = CommandsManager.CreateNotNull<EditablePhoneViewModel>(_ => AddNewPhone(), CanAddPhone);
            RemovePhoneCommand = CommandsManager.CreateNotNull<EditablePhoneViewModel>(RemovePhone, CanRemovePhone);
            RemoveAllPositionsCommand = CommandsManager.Create(() => SelectedPositions = null, () => SelectedPositions is not null);
        }

        protected override SquadPlayerDto ToDto() => new()
        {
            Id = ItemId,
            FirstName = FirstName,
            LastName = LastName,
            TeamId = TeamId,
            Category = Category,
            Photo = Photo,
            Gender = Gender,
            Number = Number.Value,
            FromDate = FromDate?.Date,
            LicenseState = LicenseState,
            LicenseNumber = LicenseNumber,
            IsMutation = IsMutation,
            Description = Description,
            Address = Address.Create(),
            Birthdate = Birthdate?.Date,
            Country = Country,
            Height = Height.Value,
            Laterality = Laterality,
            PlaceOfBirth = PlaceOfBirth,
            ShoesSize = ShoesSize.Value,
            Size = Size,
            Weight = Weight.Value,
            Phones = new List<PhoneDto>(Phones.Where(x => !string.IsNullOrEmpty(x.Value)).Select(x => new PhoneDto
            {
                Value = x.Value,
                Default = x.Default,
                Label = x.Label
            })),
            Emails = new List<EmailDto>(Emails.Where(x => !string.IsNullOrEmpty(x.Value)).Select(x => new EmailDto
            {
                Value = x.Value,
                Default = x.Default,
                Label = x.Label
            })),
            Positions = new List<RatedPositionDto>(SelectedPositions?.OfType<EditableRatedPositionViewModel>().Select(x => new RatedPositionDto
            {
                Id = x.Id,
                IsNatural = x.IsNatural,
                Position = x.Position,
                Rating = x.Rating
            }).ToArray() ?? [])
        };

        protected override void RefreshCore()
        {
            Address.Refresh();
            base.RefreshCore();
        }

        protected override void RefreshFrom(SquadPlayer item)
        {
            FirstName = item.Player.FirstName;
            LastName = item.Player.LastName;
            TeamId = item.Team?.Id;
            Category = item.Category;
            Photo = item.Player.Photo;
            Gender = item.Player.Gender;
            Number.Value = item.Number;
            FromDate = item.FromDate?.Date;
            LicenseNumber = item.Player.LicenseNumber;
            LicenseState = item.LicenseState;
            IsMutation = item.IsMutation;
            Description = item.Player.Description;
            Birthdate = item.Player.Birthdate?.Date;
            Country = item.Player.Country;
            Height.Value = item.Player.Height;
            Laterality = item.Player.Laterality;
            PlaceOfBirth = item.Player.PlaceOfBirth;
            ShoesSize.Value = item.ShoesSize;
            Size = item.Size;
            Weight.Value = item.Player.Weight;
            Address.Load(item.Player.Address);
            Phones.Set(item.Player.Phones.OrderByDescending(x => x.Default).ThenBy(x => x.Label).Select(x => new EditablePhoneViewModel
            {
                Value = x.Value,
                Default = x.Default,
                Label = x.Label
            }));
            Emails.Set(item.Player.Emails.OrderByDescending(x => x.Default).ThenBy(x => x.Label).Select(x => new EditableEmailViewModel
            {
                Value = x.Value,
                Default = x.Default,
                Label = x.Label
            }));
            AllPositions = Position.GetPlayerPositions().Select(x =>
            {
                var foundPosition = item.Positions.FirstOrDefault(y => y.Position == x);
                var editablePosition = new EditableRatedPositionViewModel(x, foundPosition?.Id)
                {
                    IsNatural = foundPosition?.IsNatural ?? false,
                    Rating = foundPosition?.Rating ?? PositionRating.VeryGood
                };

                return editablePosition;
            }).ToList().AsReadOnly();

            if (!Phones.Any()) AddNewPhone();
            if (!Emails.Any()) AddNewEmail();
            SelectedPositions = AllPositions.Where(x => item.Positions.Select(y => y.Position).Contains(x.Position)).OrderBy(x => x.Position).ToList();
        }

        protected override void ResetItem()
        {
            var defaultValues = CrudService.NewPlayer();
            FirstName = defaultValues.FirstName.OrEmpty();
            LastName = defaultValues.LastName.OrEmpty();
            TeamId = defaultValues.TeamId;
            Category = defaultValues.Category;
            IsMutation = defaultValues.IsMutation;
            Photo = defaultValues.Photo;
            Birthdate = defaultValues.Birthdate?.Date;
            FromDate = defaultValues.FromDate?.Date;
            LicenseNumber = defaultValues.LicenseNumber;
            Description = defaultValues.Description;
            Address.Load(defaultValues.Address);
            PlaceOfBirth = defaultValues.PlaceOfBirth;
            Size = defaultValues.Size;
            Gender = defaultValues.Gender;
            LicenseState = defaultValues.LicenseState;
            Laterality = defaultValues.Laterality;
            Country = defaultValues.Country;
            Number.Value = defaultValues.Number;
            ShoesSize.Value = defaultValues.ShoesSize;
            Height.Value = defaultValues.Height;
            Weight.Value = defaultValues.Weight;
            Phones.Clear();
            Emails.Clear();

            if (!Phones.Any()) AddNewPhone();
            if (!Emails.Any()) AddNewEmail();

            AllPositions = Position.GetPlayerPositions().Select(x => new EditableRatedPositionViewModel(x)).ToList().AsReadOnly();

            SelectedPositions = null;
        }

        #region Sizes

        private void DownSize(object? obj) => Size = Sizes.IndexOf(Size.OrEmpty()) > 0 ? Sizes[Sizes.IndexOf(Size.OrEmpty()) - 1] : Sizes.LastOrDefault();

        private bool CanDownSize(object? obj) => Sizes.IndexOf(Size.OrEmpty()) > 0 || Sizes.IndexOf(Size.OrEmpty()) == -1;

        private void UpSize(object? obj) => Size = Sizes.IndexOf(Size.OrEmpty()) < Sizes.Count - 1 && Sizes.IndexOf(Size.OrEmpty()) != -1
                ? Sizes[Sizes.IndexOf(Size.OrEmpty()) + 1]
                : Sizes.FirstOrDefault();

        private bool CanUpSize(object? obj) => Sizes.IndexOf(Size.OrEmpty()) < Sizes.Count - 1 || Sizes.IndexOf(Size.OrEmpty()) == -1;

        #endregion

        #region Contacts

        private void AddNewPhone() => Phones.Add(new EditablePhoneViewModel());

        private void AddNewEmail() => Emails.Add(new EditableEmailViewModel());

        private bool CanAddPhone(EditablePhoneViewModel phone) => Phones.IndexOf(phone) == Phones.Count - 1 && !string.IsNullOrEmpty(phone.Value);

        private bool CanAddEmail(EditableEmailViewModel email) => Emails.IndexOf(email) == Emails.Count - 1 && !string.IsNullOrEmpty(email.Value);

        private void RemovePhone(EditablePhoneViewModel phone)
        {
            if (Phones.Count > 1)
                _ = Phones.Remove(phone);
            else if (Phones.Count == 1)
            {
                phone.Label = string.Empty;
                phone.Value = string.Empty;
            }
        }

        private void RemoveEmail(EditableEmailViewModel email)
        {
            if (Emails.Count > 1)
                _ = Emails.Remove(email);
            else if (Emails.Count == 1)
            {
                email.Label = string.Empty;
                email.Value = string.Empty;
            }
        }

        private bool CanRemovePhone(EditablePhoneViewModel phone) => Phones.Count > 1 || !string.IsNullOrEmpty(phone?.Value);

        private bool CanRemoveEmail(EditableEmailViewModel email) => Emails.Count > 1 || !string.IsNullOrEmpty(email?.Value);

        #endregion
    }
}
