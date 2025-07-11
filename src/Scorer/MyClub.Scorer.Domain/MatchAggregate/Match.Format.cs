// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.MatchAggregate
{
    public class MatchFormat : ValueObject
    {
        public static readonly MatchFormat Default = new(HalfFormat.Default);

        public static readonly MatchFormat NoDraw = new(HalfFormat.Default, HalfFormat.ExtraTime, 5);

        public MatchFormat(HalfFormat regulationTime, HalfFormat? extraTime = null, int? numberOfPenaltyShootouts = null)
        {
            RegulationTime = regulationTime;
            ExtraTime = extraTime;
            NumberOfPenaltyShootouts = numberOfPenaltyShootouts;
        }

        public HalfFormat RegulationTime { get; }

        public HalfFormat? ExtraTime { get; }

        public bool ExtraTimeIsEnabled => ExtraTime is not null;

        public int? NumberOfPenaltyShootouts { get; }

        public bool ShootoutIsEnabled => NumberOfPenaltyShootouts > 0;

        public TimeSpan GetFullTime(bool withHalfTime = true) => RegulationTime.GetFullTime(withHalfTime) + (ExtraTime?.GetFullTime(withHalfTime) ?? TimeSpan.Zero);

        public override string ToString()
        {
            var str = new StringBuilder(RegulationTime.ToString());

            if (ExtraTimeIsEnabled)
                str.Append($" ({ExtraTime})");

            if (ShootoutIsEnabled)
                str.Append($" + {NumberOfPenaltyShootouts} penalty shootouts");

            return str.ToString();
        }
    }
}
