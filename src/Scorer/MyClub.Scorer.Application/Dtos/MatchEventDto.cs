// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Application.Dtos;

namespace MyClub.Scorer.Application.Dtos
{
    public abstract class MatchEventDto : EntityDto
    {
        public int? Minute { get; set; }
    }
}
