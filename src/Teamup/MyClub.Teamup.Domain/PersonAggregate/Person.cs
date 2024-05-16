// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Domain.Exceptions;
using MyNet.Utilities;
using MyNet.Utilities.Extensions;
using MyNet.Utilities.Geography;

namespace MyClub.Teamup.Domain.PersonAggregate
{
    public class Person : AuditableEntity, IAggregateRoot
    {
        private DateTime? _birthdate;
        private string _lastName = null!;
        private string _firstName = null!;
        private readonly ObservableCollection<Email> _emails = [];
        private readonly ObservableCollection<Phone> _phones = [];

        protected Person(string firstName, string lastName, Guid? id = null) : base(id)
        {
            FirstName = firstName;
            LastName = lastName;
            Emails = new(_emails);
            Phones = new(_phones);
        }

        public string LastName
        {
            get => _lastName;
            set => _lastName = value.IsRequiredOrThrow();
        }

        public string FirstName
        {
            get => _firstName;
            set => _firstName = value.IsRequiredOrThrow();
        }

        public DateTime? Birthdate
        {
            get => _birthdate;
            set => _birthdate = value.IsInPastOrThrow()?.Date;
        }

        public string? PlaceOfBirth { get; set; }

        public Category? Category { get; set; }

        public Country? Country { get; set; }

        public byte[]? Photo { get; set; }

        public GenderType Gender { get; set; } = GenderType.Male;

        public Address? Address { get; set; }

        public string? Description { get; set; }

        public string? LicenseNumber { get; set; }

        public ReadOnlyObservableCollection<Email> Emails { get; }

        public ReadOnlyObservableCollection<Phone> Phones { get; }

        public string GetInverseName() => string.Join(" ", LastName, FirstName);

        public string GetFullName() => string.Join(" ", FirstName, LastName);

        public int? GetAge() => !Birthdate.HasValue ? null : Birthdate.Value.GetAge();

        public Phone AddPhone(string value, string? label = null, bool isDefault = false) => AddPhone(new Phone(value, label, isDefault));

        public Phone AddPhone(Phone phone)
        {
            if (_phones.Any(x => x.Value == phone.Value))
                throw new AlreadyExistsException(nameof(Phones), phone.Value);

            _phones.Add(phone);

            return phone;
        }

        public Email AddEmail(string value, string? label = null, bool isDefault = false) => AddEmail(new Email(value, label, isDefault));

        public Email AddEmail(Email email)
        {
            if (_emails.Any(x => x.Value == email.Value))
                throw new AlreadyExistsException(nameof(Emails), email.Value);

            _emails.Add(email);

            return email;
        }

        public bool RemoveEmail(string value)
            => _emails.Any(x => x.Value == value) && _emails.Remove(_emails.First(x => x.Value == value));

        public bool RemovePhone(string value)
            => _phones.Any(x => x.Value == value) && _phones.Remove(_phones.First(x => x.Value == value));

        public void ClearEmails() => _emails.Clear();

        public void ClearPhones() => _phones.Clear();

        public override string? ToString() => GetInverseName();

        public override int CompareTo(object? obj) => obj is not Person other ? -1 : GetInverseName() != other.GetInverseName() ? string.Compare(GetInverseName(), other.GetInverseName(), StringComparison.OrdinalIgnoreCase) : base.CompareTo(obj);

        public bool IsSimilar(object? obj) => obj is Person person && GetInverseName().Equals(person.GetInverseName(), StringComparison.OrdinalIgnoreCase);
    }
}
