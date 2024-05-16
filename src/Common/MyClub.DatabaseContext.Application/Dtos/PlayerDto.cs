// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Application.Dtos;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyNet.Utilities;
using MyNet.Utilities.Geography;

namespace MyClub.DatabaseContext.Application.Dtos
{
    public class PlayerDto : EntityDto
    {
        public string? LastName { get; set; }

        public string? FirstName { get; set; }

        public DateTime? Birthdate { get; set; }

        public DateTime? FromDate { get; set; }

        public string? PlaceOfBirth { get; set; }

        public Country? Country { get; set; }

        public byte[]? Photo { get; set; }

        public GenderType Gender { get; set; }

        public Address? Address { get; set; }

        public string? LicenseNumber { get; set; }

        public string? Description { get; set; }

        public string? Size { get; set; }

        public List<Email>? Emails { get; set; }

        public List<Phone>? Phones { get; set; }

        public Laterality Laterality { get; set; }

        public int? Height { get; set; }

        public int? Weight { get; set; }

        public List<RatedPositionDto>? Positions { get; set; }

        public List<InjuryDto>? Injuries { get; set; }

        public Category? Category { get; set; }
    }
}
