// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.Utilities;

namespace MyClub.Scorer.Domain.RankingAggregate
{
    public class RankLabel : ValueObject
    {
        public RankLabel(string? color, string name, string shortName, string? description = null)
        {
            Color = color;
            Name = name;
            ShortName = shortName;
            Description = description;
        }

        public string? Color { get; }

        public string Name { get; }

        public string ShortName { get; }

        public string? Description { get; }
    }
}
