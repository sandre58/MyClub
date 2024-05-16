// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Application.Dtos;
using MyNet.Utilities;
using MyNet.Utilities.Geography;

namespace MyClub.Teamup.Application.Dtos
{
    public class PersonDto : EntityDto
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

        public List<EmailDto>? Emails { get; set; }

        public List<PhoneDto>? Phones { get; set; }
    }
}
