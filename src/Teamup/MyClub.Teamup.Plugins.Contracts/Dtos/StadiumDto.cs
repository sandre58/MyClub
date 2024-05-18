// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

namespace MyClub.Teamup.Plugins.Contracts.Dtos
{
    public class StadiumDto
    {
        public string? Name { get; set; }

        public string? Ground { get; set; }

        public string? Street { get; set; }

        public string? PostalCode { get; set; }

        public string? City { get; set; }

        public string? Country { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }
    }
}
