// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Application.Dtos;
using MyNet.Utilities;
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Application.Dtos
{
    public class PersonDto : EntityDto
    {
        public string? LastName { get; set; }

        public string? FirstName { get; set; }

        public Country? Country { get; set; }

        public byte[]? Photo { get; set; }

        public GenderType Gender { get; set; }

        public string? LicenseNumber { get; set; }

        public string? Email { get; set; }
    }
}
