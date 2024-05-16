// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Application.Dtos
{
    public class StadiumExportDto : StadiumDto
    {
        public string? Street { get; set; }

        public string? PostalCode { get; set; }

        public string? City { get; set; }

        public Country? Country { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

    }
}
