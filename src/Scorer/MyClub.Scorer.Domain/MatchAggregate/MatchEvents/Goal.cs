// -----------------------------------------------------------------------
// <copyright file="Goal.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using System.Text;
using MyClub.Scorer.Domain.CompetitionAggregate.Teams;
using MyClub.Shared.Domain.Enums;

namespace MyClub.Scorer.Domain.MatchAggregate.MatchEvents;

public class Goal : MatchEvent
{
    // <remarks>Used by EF Core</remarks>
    private Goal()
        : base() { }

    private Goal(MatchEventId id, GoalType type, PlayerId? scorerId = null, PlayerId? assistId = null, int? minute = null)
        : base(id, minute)
    {
        Type = type;
        ScorerId = scorerId;
        AssistId = assistId;
    }

    public static Goal Create(GoalType type, PlayerId? scorerId = null, PlayerId? assistId = null, int? minute = null) => new(MatchEventId.New(), type, scorerId, assistId, minute);

    public static Goal Penalty(PlayerId? scorerId = null, int? minute = null) => Create(GoalType.Penalty, scorerId, minute: minute);

    public static Goal FreeKick(PlayerId? scorerId = null, int? minute = null) => Create(GoalType.FreeKick, scorerId, minute: minute);

    public static Goal OwnGoal(int? minute = null) => Create(GoalType.OwnGoal, minute: minute);

    public GoalType Type { get; set; }

    public PlayerId? ScorerId { get; set; }

    public PlayerId? AssistId { get; set; }

    public override string ToString()
    {
        var str = new StringBuilder();

        if (Minute.HasValue)
            _ = str.Append(CultureInfo.CurrentCulture, $"{Minute.Value}' : ");

        if (Type == GoalType.OwnGoal)
        {
            _ = str.Append(CultureInfo.CurrentCulture, $"OG");

            if (ScorerId is not null)
                _ = str.Append(CultureInfo.CurrentCulture, $" ({ScorerId})");
        }
        else
        {
            if (ScorerId is not null)
                _ = str.Append(CultureInfo.CurrentCulture, $"{ScorerId}");

            if (Type == GoalType.Regular && AssistId is not null)
                _ = str.Append(CultureInfo.CurrentCulture, $" ({AssistId})");
            else if (Type == GoalType.Penalty)
                _ = str.Append(" (p)");
        }

        return str.ToString();
    }
}
