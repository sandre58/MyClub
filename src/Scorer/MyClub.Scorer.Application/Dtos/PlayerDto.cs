// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;

namespace MyClub.Scorer.Application.Dtos
{
    public class PlayerDto : PersonDto
    {
        public Guid TeamId { get; set; }
    }
}
