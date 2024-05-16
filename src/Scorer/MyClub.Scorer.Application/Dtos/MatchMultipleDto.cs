// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyNet.Utilities.Units;

namespace MyClub.Scorer.Application.Dtos
{
    public class MatchMultipleDto : MatchDto
    {
        public bool UpdateDate { get; set; }

        public bool UpdateTime { get; set; }

        public bool UpdateStadium { get; set; }

        public TimeSpan Time { get; set; }

        public int Offset { get; set; }

        public TimeUnit OffsetUnit { get; set; }
    }
}
