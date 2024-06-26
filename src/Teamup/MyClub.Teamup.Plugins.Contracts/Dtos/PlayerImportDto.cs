﻿// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace MyClub.Teamup.Plugins.Contracts.Dtos
{
    public class PlayerImportDto
    {
        public string? LastName { get; set; }

        public string? FirstName { get; set; }

        public DateTime? Birthdate { get; set; }

        public DateTime? FromDate { get; set; }

        public string? PlaceOfBirth { get; set; }

        public string? Country { get; set; }

        public byte[]? Photo { get; set; }

        public string? Gender { get; set; }

        public string? Street { get; set; }

        public string? PostalCode { get; set; }

        public string? City { get; set; }

        public string? AddressCountry { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public string? LicenseNumber { get; set; }

        public string? Description { get; set; }

        public string? Size { get; set; }

        public string? Laterality { get; set; }

        public int? Height { get; set; }

        public int? Weight { get; set; }

        public List<RatedPositionImportDto>? Positions { get; set; }

        public List<InjuryImportDto>? Injuries { get; set; }

        public List<ContactImportDto>? Emails { get; set; }

        public List<ContactImportDto>? Phones { get; set; }

        public string? Category { get; set; }

        public int? Number { get; set; }

        public string? LicenseState { get; set; }

        public bool IsMutation { get; set; }

        public int? ShoesSize { get; set; }
    }
}
