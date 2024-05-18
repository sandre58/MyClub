// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;

namespace MyClub.Teamup.Plugins.Contracts.Dtos
{
    public class CompetitionDto
    {
        public string? Name { get; set; }

        public string? ShortName { get; set; }

        public byte[]? Logo { get; set; }

        public string? Category { get; set; }

        public int RegulationTimeNumber { get; set; }

        public TimeSpan RegulationTimeDuration { get; set; }

        public int? ExtraTimeNumber { get; set; }

        public TimeSpan? ExtraTimeDuration { get; set; }

        public int? NumberOfPenaltyShootouts { get; set; }

        public TimeSpan MatchTime { get; set; }
    }
}
