// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Application.Dtos;
using MyClub.Domain.Enums;

namespace MyClub.DatabaseContext.Application.Dtos
{
    public class RatedPositionDto : EntityDto
    {
        public Position? Position { get; set; }

        public PositionRating Rating { get; set; }

        public bool IsNatural { get; set; }
    }
}
