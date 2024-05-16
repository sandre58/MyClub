// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain.Enums;
using MyClub.Teamup.Domain.TacticAggregate;
using MyNet.Utilities;

namespace MyClub.Teamup.Domain.Enums
{
    public class KnownTactic : Enumeration<KnownTactic>, ISimilar<KnownTactic>
    {
        private static int _value = 1;
        private readonly Func<TacticPosition[]> _create;

        public static readonly KnownTactic _343 = new(nameof(_343), Create343);
        public static readonly KnownTactic _4222 = new(nameof(_4222), Create4222);
        public static readonly KnownTactic _4231 = new(nameof(_4231), Create4231);
        public static readonly KnownTactic _424 = new(nameof(_424), Create424);
        public static readonly KnownTactic _4321 = new(nameof(_4321), Create4321);
        public static readonly KnownTactic _433Defensive = new(nameof(_433Defensive), Create433Defensive);
        public static readonly KnownTactic _433Offensive = new(nameof(_433Offensive), Create433Offensive);
        public static readonly KnownTactic _442 = new(nameof(_442), Create442);
        public static readonly KnownTactic _442Diamond = new(nameof(_442Diamond), Create442Diamond);
        public static readonly KnownTactic _5212 = new(nameof(_5212), Create5212);
        public static readonly KnownTactic _5221 = new(nameof(_5221), Create5221);
        public static readonly KnownTactic _523 = new(nameof(_523), Create523);
        public static readonly KnownTactic _532 = new(nameof(_532), Create532);

        public TacticPosition[] GetPositions() => _create();

        private KnownTactic(string name, Func<TacticPosition[]> create)
            : base(name, _value, $"{nameof(KnownTactic)}{name}")
        {
            _create = create;

            IncrementValue();
        }

        private static void IncrementValue() => _value++;

        public bool IsSimilar(KnownTactic? obj) => obj is not null && obj == this;

        public static TacticPosition[] Create343()
            => [
                new TacticPosition(Position.GoalKeeper) { Number = 1 },

                new TacticPosition(Position.CenterLeftBack) { Number = 3 },
                new TacticPosition(Position.CenterBack) { Number = 4 },
                new TacticPosition(Position.CenterRightBack) { Number = 2 },

                new TacticPosition(Position.LeftMidfielder) { Number = 10 },
                new TacticPosition(Position.CenterLeftMidfielder) { Number = 6 },
                new TacticPosition(Position.CenterRightMidfielder) { Number = 5 },
                new TacticPosition(Position.RightMidfielder) { Number = 8 },

                new TacticPosition(Position.LeftForward) { Number = 11 },
                new TacticPosition(Position.Forward) { Number = 9 },
                new TacticPosition(Position.RightForward) { Number = 7 }
            ];

        public static TacticPosition[] Create4222()
            => [
                new TacticPosition(Position.GoalKeeper) { Number = 1 },

                new TacticPosition(Position.LeftFullBack) { Number = 3 },
                new TacticPosition(Position.CenterLeftBack) { Number = 4 },
                new TacticPosition(Position.CenterRightBack) { Number = 5 },
                new TacticPosition(Position.RightFullBack) { Number = 2 },

                new TacticPosition(Position.DefensiveLeftMidfielder) { Number = 6 },
                new TacticPosition(Position.DefensiveRightMidfielder) { Number = 8 },

                new TacticPosition(Position.CenterLeftAttackingMidfielder) { Number = 10 },
                new TacticPosition(Position.CenterRightAttackingMidfielder) { Number = 7 },

                new TacticPosition(Position.LeftForward) { Number = 11 },
                new TacticPosition(Position.RightForward) { Number = 9 },
            ];

        public static TacticPosition[] Create4231()
            => [
                new TacticPosition(Position.GoalKeeper) { Number = 1 },

                new TacticPosition(Position.LeftFullBack) { Number = 3 },
                new TacticPosition(Position.CenterLeftBack) { Number = 4 },
                new TacticPosition(Position.CenterRightBack) { Number = 5 },
                new TacticPosition(Position.RightFullBack) { Number = 2 },

                new TacticPosition(Position.DefensiveLeftMidfielder) { Number = 6 },
                new TacticPosition(Position.DefensiveRightMidfielder) { Number = 8 },

                new TacticPosition(Position.LeftAttackingMidfielder) { Number = 11 },
                new TacticPosition(Position.CenterAttackingMidfielder) { Number = 10 },
                new TacticPosition(Position.RightAttackingMidfielder) { Number = 7 },

                new TacticPosition(Position.Forward) { Number = 9 },
            ];

        public static TacticPosition[] Create424()
            => [
                new TacticPosition(Position.GoalKeeper) { Number = 1 },

                new TacticPosition(Position.LeftFullBack) { Number = 3 },
                new TacticPosition(Position.CenterLeftBack) { Number = 4 },
                new TacticPosition(Position.CenterRightBack) { Number = 5 },
                new TacticPosition(Position.RightFullBack) { Number = 2 },

                new TacticPosition(Position.CenterLeftMidfielder) { Number = 6 },
                new TacticPosition(Position.CenterRightMidfielder) { Number = 8 },

                new TacticPosition(Position.LeftAttackingMidfielder) { Number = 10 },
                new TacticPosition(Position.LeftForward) { Number = 11 },
                new TacticPosition(Position.RightForward) { Number = 9 },
                new TacticPosition(Position.RightAttackingMidfielder) { Number = 7 },
            ];

        public static TacticPosition[] Create4321()
            => [
                new TacticPosition(Position.GoalKeeper) { Number = 1 },

                new TacticPosition(Position.LeftFullBack) { Number = 3 },
                new TacticPosition(Position.CenterLeftBack) { Number = 4 },
                new TacticPosition(Position.CenterRightBack) { Number = 5 },
                new TacticPosition(Position.RightFullBack) { Number = 2 },

                new TacticPosition(Position.DefensiveMidfielder) { Number = 6 },
                new TacticPosition(Position.CenterLeftMidfielder) { Number = 10 },
                new TacticPosition(Position.CenterRightMidfielder) { Number = 8 },

                new TacticPosition(Position.CenterLeftAttackingMidfielder) { Number = 11 },
                new TacticPosition(Position.Forward) { Number = 9 },
                new TacticPosition(Position.CenterRightAttackingMidfielder) { Number = 7 },
            ];

        public static TacticPosition[] Create433Defensive()
            => [
                new TacticPosition(Position.GoalKeeper) { Number = 1 },

                new TacticPosition(Position.LeftFullBack) { Number = 3 },
                new TacticPosition(Position.CenterLeftBack) { Number = 4 },
                new TacticPosition(Position.CenterRightBack) { Number = 5 },
                new TacticPosition(Position.RightFullBack) { Number = 2 },

                new TacticPosition(Position.DefensiveMidfielder) { Number = 6 },
                new TacticPosition(Position.CenterLeftMidfielder) { Number = 10 },
                new TacticPosition(Position.CenterRightMidfielder) { Number = 8 },

                new TacticPosition(Position.LeftAttackingMidfielder) { Number = 11 },
                new TacticPosition(Position.Forward) { Number = 9 },
                new TacticPosition(Position.RightAttackingMidfielder) { Number = 7 },
            ];

        public static TacticPosition[] Create433Offensive()
            => [
                new TacticPosition(Position.GoalKeeper) { Number = 1 },

                new TacticPosition(Position.LeftFullBack) { Number = 3 },
                new TacticPosition(Position.CenterLeftBack) { Number = 4 },
                new TacticPosition(Position.CenterRightBack) { Number = 5 },
                new TacticPosition(Position.RightFullBack) { Number = 2 },

                new TacticPosition(Position.CenterLeftMidfielder) { Number = 6 },
                new TacticPosition(Position.CenterRightMidfielder) { Number = 8 },
                new TacticPosition(Position.CenterAttackingMidfielder) { Number = 10 },

                new TacticPosition(Position.LeftAttackingMidfielder) { Number = 11 },
                new TacticPosition(Position.Forward) { Number = 9 },
                new TacticPosition(Position.RightAttackingMidfielder) { Number = 7 },
            ];

        public static TacticPosition[] Create442()
            => [
                new TacticPosition(Position.GoalKeeper) { Number = 1 },

                new TacticPosition(Position.LeftFullBack) { Number = 3 },
                new TacticPosition(Position.CenterLeftBack) { Number = 4 },
                new TacticPosition(Position.CenterRightBack) { Number = 5 },
                new TacticPosition(Position.RightFullBack) { Number = 2 },

                new TacticPosition(Position.LeftMidfielder) { Number = 10 },
                new TacticPosition(Position.CenterLeftMidfielder) { Number = 6 },
                new TacticPosition(Position.CenterRightMidfielder) { Number = 8 },
                new TacticPosition(Position.RightMidfielder) { Number = 7 },

                new TacticPosition(Position.LeftForward) { Number = 11 },
                new TacticPosition(Position.RightForward) { Number = 9 },
            ];

        public static TacticPosition[] Create442Diamond()
            => [
                new TacticPosition(Position.GoalKeeper) { Number = 1 },

                new TacticPosition(Position.LeftFullBack) { Number = 3 },
                new TacticPosition(Position.CenterLeftBack) { Number = 4 },
                new TacticPosition(Position.CenterRightBack) { Number = 5 },
                new TacticPosition(Position.RightFullBack) { Number = 2 },

                new TacticPosition(Position.DefensiveMidfielder) { Number = 6 },
                new TacticPosition(Position.CenterLeftMidfielder) { Number = 7 },
                new TacticPosition(Position.CenterRightMidfielder) { Number = 8 },
                new TacticPosition(Position.CenterAttackingMidfielder) { Number = 10 },

                new TacticPosition(Position.LeftForward) { Number = 11 },
                new TacticPosition(Position.RightForward) { Number = 9 },
            ];

        public static TacticPosition[] Create5212()
            => [
                new TacticPosition(Position.GoalKeeper) { Number = 1 },

                new TacticPosition(Position.LeftWingBack) { Number = 7 },
                new TacticPosition(Position.CenterLeftBack) { Number = 3 },
                new TacticPosition(Position.CenterBack) { Number = 5 },
                new TacticPosition(Position.CenterRightBack) { Number = 4 },
                new TacticPosition(Position.RightWingBack) { Number = 2 },

                new TacticPosition(Position.DefensiveLeftMidfielder) { Number = 6 },
                new TacticPosition(Position.DefensiveRightMidfielder) { Number = 8 },

                new TacticPosition(Position.CenterAttackingMidfielder) { Number = 10 },

                new TacticPosition(Position.LeftForward) { Number = 11 },
                new TacticPosition(Position.RightForward) { Number = 9 },
            ];

        public static TacticPosition[] Create5221()
            => [
                new TacticPosition(Position.GoalKeeper) { Number = 1 },

                new TacticPosition(Position.LeftWingBack) { Number = 7 },
                new TacticPosition(Position.CenterLeftBack) { Number = 3 },
                new TacticPosition(Position.CenterBack) { Number = 5 },
                new TacticPosition(Position.CenterRightBack) { Number = 4 },
                new TacticPosition(Position.RightWingBack) { Number = 2 },

                new TacticPosition(Position.DefensiveLeftMidfielder) { Number = 6 },
                new TacticPosition(Position.DefensiveRightMidfielder) { Number = 8 },

                new TacticPosition(Position.CenterLeftAttackingMidfielder) { Number = 10 },
                new TacticPosition(Position.CenterRightAttackingMidfielder) { Number = 11 },

                new TacticPosition(Position.Forward) { Number = 9 },
            ];

        public static TacticPosition[] Create523()
            => [
                new TacticPosition(Position.GoalKeeper) { Number = 1 },

                new TacticPosition(Position.LeftWingBack) { Number = 7 },
                new TacticPosition(Position.CenterLeftBack) { Number = 3 },
                new TacticPosition(Position.CenterBack) { Number = 5 },
                new TacticPosition(Position.CenterRightBack) { Number = 4 },
                new TacticPosition(Position.RightWingBack) { Number = 2 },

                new TacticPosition(Position.DefensiveLeftMidfielder) { Number = 6 },
                new TacticPosition(Position.DefensiveRightMidfielder) { Number = 8 },

                new TacticPosition(Position.LeftAttackingMidfielder) { Number = 11 },
                new TacticPosition(Position.Forward) { Number = 9 },
                new TacticPosition(Position.RightAttackingMidfielder) { Number = 7 },
            ];

        public static TacticPosition[] Create532()
        => [
        new TacticPosition(Position.GoalKeeper) { Number = 1 },

            new TacticPosition(Position.LeftWingBack) { Number = 7 },
            new TacticPosition(Position.CenterLeftBack) { Number = 3 },
            new TacticPosition(Position.CenterBack) { Number = 5 },
            new TacticPosition(Position.CenterRightBack) { Number = 4 },
            new TacticPosition(Position.RightWingBack) { Number = 2 },

            new TacticPosition(Position.DefensiveMidfielder) { Number = 6 },
            new TacticPosition(Position.CenterLeftMidfielder) { Number = 8 },
            new TacticPosition(Position.CenterRightMidfielder) { Number = 10 },

            new TacticPosition(Position.LeftForward) { Number = 11 },
            new TacticPosition(Position.RightForward) { Number = 9 },
        ];
    }
}
