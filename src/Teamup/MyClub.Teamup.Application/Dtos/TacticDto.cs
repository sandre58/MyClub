// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Application.Dtos;

namespace MyClub.Teamup.Application.Dtos
{
    public class TacticDto : EntityDto
    {
        public string? Code { get; set; }

        public string? Label { get; set; }

        public int Order { get; set; }

        public string? Description { get; set; }

        public List<TacticPositionDto>? Positions { get; set; }

        public List<string> Instructions { get; set; } = [];
    }
}
