// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;

namespace MyClub.DatabaseContext.Domain.CompetitionAggregate
{
    public class Competition : Entity
    {
        public const string League = "League";

        public const string Cup = "Cup";

        public const string Friendly = "Friendly";

        public Competition(string name, string shortName, string type)
        {
            Name = name;
            ShortName = shortName;
            Type = type;
        }

        public Competition(Guid id, string name, string shortName, string type) : base(id)
        {
            Name = name;
            ShortName = shortName;
            Type = type;
        }

        public string Name { get; set; }

        public string ShortName { get; set; }

        public string? Country { get; set; }

        public string? Category { get; set; }

        public bool IsNational { get; set; }

        public byte[]? Logo { get; set; }

        public string Type { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public TimeSpan? MatchTime { get; set; }

        public string? Description { get; set; }

        public int RegulationTimeNumber { get; set; }

        public TimeSpan RegulationTimeDuration { get; set; }

        public int? ExtraTimeNumber { get; set; }

        public TimeSpan? ExtraTimeDuration { get; set; }

        public int? NumberOfPenaltyShootouts { get; set; }

        public int? PointsByGamesWon { get; set; }

        public int? PointsByGamesDrawn { get; set; }

        public int? PointsByGamesLost { get; set; }

        public string? SortingColumns { get; set; }

        public string? RankLabels { get; set; }

        public override string ToString() => Name;

        public override bool IsSimilar(object? obj) => obj is Competition other && Name == other.Name && Country == other.Country && Category == other.Category;

        public override void SetFrom(object? from)
        {
            if (from is Competition competition)
            {
                Name = competition.Name;
                ShortName = competition.ShortName;
                Logo = competition.Logo;
                StartDate = competition.StartDate;
                EndDate = competition.EndDate;
                RegulationTimeDuration = competition.RegulationTimeDuration;
                RegulationTimeNumber = competition.RegulationTimeNumber;
                ExtraTimeDuration = competition.ExtraTimeDuration;
                ExtraTimeNumber = competition.ExtraTimeNumber;
                NumberOfPenaltyShootouts = competition.NumberOfPenaltyShootouts;
                PointsByGamesDrawn = competition.PointsByGamesDrawn;
                PointsByGamesLost = competition.PointsByGamesLost;
                PointsByGamesWon = competition.PointsByGamesWon;
                SortingColumns = competition.SortingColumns;
                RankLabels = competition.RankLabels;
            }
        }
    }
}
