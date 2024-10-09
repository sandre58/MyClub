// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Application.Dtos;
using MyClub.Domain.Enums;

namespace MyClub.Scorer.Application.Dtos
{
    public class PenaltyShootoutDto : EntityDto
    {
        public Guid? TakerId { get; set; }

        public PenaltyShootoutResult Result { get; set; }
    }
}
