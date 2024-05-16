// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

namespace MyClub.Teamup.Application.Dtos
{
    public class PlayerMultipleDto : SquadPlayerDto
    {
        public bool UpdateCategory { get; set; }

        public bool UpdateCountry { get; set; }

        public bool UpdateIsMutation { get; set; }

        public bool UpdateLicenseState { get; set; }

        public bool UpdateSize { get; set; }

        public bool UpdateShoesSize { get; set; }
    }
}
