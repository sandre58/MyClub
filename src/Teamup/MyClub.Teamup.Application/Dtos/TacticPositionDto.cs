// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Application.Dtos;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Application.Dtos
{
    public class TacticPositionDto : EntityDto
    {
        public Position? Position { get; set; }

        public double OffsetX { get; set; }

        public double OffsetY { get; set; }

        public int? Number { get; set; }

        public List<string> Instructions { get; set; } = [];
    }
}
