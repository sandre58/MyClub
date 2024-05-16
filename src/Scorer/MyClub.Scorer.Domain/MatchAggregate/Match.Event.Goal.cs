// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using MyClub.Scorer.Domain.PersonAggregate;
using MyClub.Domain.Enums;

namespace MyClub.Scorer.Domain.MatchAggregate
{
    public class Goal : MatchEvent
    {
        public static Goal Penalty(Player? scorer = null, int? minute = null) => new(GoalType.Penalty, scorer, minute: minute);

        public static Goal FreeKick(Player? scorer = null, int? minute = null) => new(GoalType.FreeKick, scorer, minute: minute);

        public static Goal OwnGoal(int? minute = null) => new(GoalType.OwnGoal, minute: minute);

        public Goal(GoalType type, Player? scorer = null, Player? assist = null, int? minute = null, Guid? id = null) : base(minute, id)
        {
            Scorer = scorer;
            Assist = assist;
            Type = type;
        }

        public GoalType Type { get; set; }

        public Player? Scorer { get; set; }

        public Player? Assist { get; set; }

        public override string ToString()
        {
            var str = new StringBuilder();

            if (Minute.HasValue)
                str.Append($"{Minute.Value}' : ");

            if (Type == GoalType.OwnGoal)
            {
                str.Append($"OG");

                if (Scorer is not null)
                    str.Append($" ({Scorer})");
            }
            else
            {
                if (Scorer is not null)
                    str.Append($"{Scorer}");

                if (Type == GoalType.Other && Assist is not null)
                    str.Append($" ({Assist})");
                else if (Type == GoalType.Penalty)
                    str.Append(" (p)");

            }

            return str.ToString();
        }
    }
}
