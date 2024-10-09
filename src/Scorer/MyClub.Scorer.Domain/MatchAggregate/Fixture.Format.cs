// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using MyClub.Scorer.Domain.Enums;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.MatchAggregate
{

    public class FixtureFormat : ValueObject
    {
        public static readonly FixtureFormat Default = new(HalfFormat.Default, NoDrawUsage.OnLastMatch, NoDrawUsage.OnLastMatch, HalfFormat.ExtraTime, 5);

        public FixtureFormat(HalfFormat regulationTime, NoDrawUsage useExtraTime, NoDrawUsage useShootout, HalfFormat? extraTime = null, int? numberOfPenaltyShootouts = null)
        {
            RegulationTime = regulationTime;
            UseExtraTime = useExtraTime;
            UseShootout = useShootout;
            ExtraTime = extraTime;
            NumberOfPenaltyShootouts = numberOfPenaltyShootouts;
        }

        public HalfFormat RegulationTime { get; }

        public HalfFormat? ExtraTime { get; }

        public NoDrawUsage UseExtraTime { get; }

        public int? NumberOfPenaltyShootouts { get; }

        public NoDrawUsage UseShootout { get; }

        public TimeSpan GetFullTime(bool withHalfTime = true) => RegulationTime.GetFullTime(withHalfTime) + (ExtraTime?.GetFullTime(withHalfTime) ?? TimeSpan.Zero);

        public override string ToString()
        {
            var str = new StringBuilder(RegulationTime.ToString());

            if (ExtraTime is not null)
                str.Append($" ({ExtraTime})");

            if (NumberOfPenaltyShootouts.HasValue)
                str.Append($" + {NumberOfPenaltyShootouts} penalty shootouts");
            return str.ToString();
        }

        public static implicit operator MatchFormat(FixtureFormat format) => new(format.RegulationTime, format.ExtraTime, format.NumberOfPenaltyShootouts);
    }
}
