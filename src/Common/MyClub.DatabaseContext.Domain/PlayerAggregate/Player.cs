// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace MyClub.DatabaseContext.Domain.PlayerAggregate
{
    public class Player : Entity
    {
        public Player(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
        }

        public Player(Guid id, string firstName, string lastName) : base(id)
        {
            FirstName = firstName;
            LastName = lastName;
        }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public DateTime? Birthdate { get; set; }

        public string? PlaceOfBirth { get; set; }

        public string? Country { get; set; }

        public string? Category { get; set; }

        public byte[]? Photo { get; set; }

        public string Gender { get; set; } = string.Empty;

        public string Laterality { get; set; } = string.Empty;

        public string? Street { get; set; }

        public string? PostalCode { get; set; }

        public string? City { get; set; }

        public string? AddressCountry { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public string? Description { get; set; }

        public string? LicenseNumber { get; set; }

        public int? Height { get; set; }

        public int? Weight { get; set; }

        public string? Size { get; set; }

        public ICollection<Email>? Emails { get; set; }

        public ICollection<Phone>? Phones { get; set; }

        public ICollection<RatedPosition>? Positions { get; set; }

        public ICollection<Injury>? Injuries { get; set; }

        public override string ToString() => string.Join(" ", LastName, FirstName);

        public override bool IsSimilar(object? obj) => obj is Player other && LastName == other.LastName && FirstName == other.FirstName;

        public override void SetFrom(object? from)
        {
            if (from is Player player)
            {
                LastName = player.LastName;
                FirstName = player.FirstName;
                PlaceOfBirth = player.PlaceOfBirth;
                Country = player.Country;
                Category = player.Category;
                Photo = player.Photo;
                Laterality = player.Laterality;
                Street = player.Street;
                PostalCode = player.PostalCode;
                City = player.City;
                AddressCountry = player.AddressCountry;
                Latitude = player.Latitude;
                Longitude = player.Longitude;
                LicenseNumber = player.LicenseNumber;
                Height = player.Height;
                Weight = player.Weight;
                Size = player.Size;
            }
        }
    }
}
