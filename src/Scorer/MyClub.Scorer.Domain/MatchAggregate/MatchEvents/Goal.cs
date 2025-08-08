// -----------------------------------------------------------------------
// <copyright file="Goal.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using System.Text;
using JetBrains.Annotations;
using MyClub.Scorer.Domain.CompetitionAggregate.Teams;
using MyClub.Shared.Domain.Enums;

namespace MyClub.Scorer.Domain.MatchAggregate.MatchEvents;

/// <summary>
/// Represents a goal scored during a football match.
/// This match event tracks when a team scores, including details about the type of goal,
/// the scorer, and any player who provided an assist.
/// </summary>
public class Goal : MatchEvent
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private Goal() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Goal"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the goal event.</param>
    /// <param name="type">The type of goal scored (regular, penalty, free kick, own goal).</param>
    /// <param name="scorerId">The identifier of the player who scored the goal. Can be null for unattributed goals.</param>
    /// <param name="assistId">The identifier of the player who provided the assist. Can be null if no assist was recorded.</param>
    /// <param name="minute">The minute of the match when the goal was scored. Can be null if timing is not recorded.</param>
    private Goal(MatchEventId id, GoalType type, PlayerId? scorerId = null, PlayerId? assistId = null, int? minute = null)
        : base(id, minute)
    {
        Type = type;
        ScorerId = scorerId;
        AssistId = assistId;
    }

    /// <summary>
    /// Creates a new goal with the specified parameters.
    /// </summary>
    /// <param name="type">The type of goal scored.</param>
    /// <param name="scorerId">The identifier of the player who scored the goal. Optional.</param>
    /// <param name="assistId">The identifier of the player who provided the assist. Optional.</param>
    /// <param name="minute">The minute of the match when the goal was scored. Optional.</param>
    /// <returns>A new <see cref="Goal"/> instance.</returns>
    public static Goal Create(GoalType type, PlayerId? scorerId = null, PlayerId? assistId = null, int? minute = null) => new(MatchEventId.New(), type, scorerId, assistId, minute);

    /// <summary>
    /// Creates a penalty goal with the specified parameters.
    /// This is a convenience method for creating penalty kick goals.
    /// </summary>
    /// <param name="scorerId">The identifier of the player who scored the penalty. Optional.</param>
    /// <param name="minute">The minute of the match when the penalty was scored. Optional.</param>
    /// <returns>A new penalty <see cref="Goal"/> instance.</returns>
    public static Goal Penalty(PlayerId? scorerId = null, int? minute = null) => Create(GoalType.Penalty, scorerId, minute: minute);

    /// <summary>
    /// Creates a free kick goal with the specified parameters.
    /// This is a convenience method for creating direct free kick goals.
    /// </summary>
    /// <param name="scorerId">The identifier of the player who scored the free kick. Optional.</param>
    /// <param name="minute">The minute of the match when the free kick was scored. Optional.</param>
    /// <returns>A new free kick <see cref="Goal"/> instance.</returns>
    public static Goal FreeKick(PlayerId? scorerId = null, int? minute = null) => Create(GoalType.FreeKick, scorerId, minute: minute);

    /// <summary>
    /// Creates an own goal with the specified parameters.
    /// This is a convenience method for creating own goal events.
    /// </summary>
    /// <param name="minute">The minute of the match when the own goal was scored. Optional.</param>
    /// <returns>A new own goal <see cref="Goal"/> instance.</returns>
    /// <remarks>
    /// Own goals typically don't have an assist since they are scored by the defending team.
    /// The scorer ID can still be set to identify which player scored the own goal.
    /// </remarks>
    public static Goal OwnGoal(int? minute = null) => Create(GoalType.OwnGoal, minute: minute);

    /// <summary>
    /// Gets or sets the type of goal scored.
    /// This determines how the goal is categorized and displayed.
    /// </summary>
    public GoalType Type { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the player who scored the goal.
    /// Can be null for goals where the scorer is not identified or tracked.
    /// </summary>
    public PlayerId? ScorerId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the player who provided the assist for the goal.
    /// Can be null for goals without assists or where the assist is not tracked.
    /// </summary>
    /// <remarks>
    /// Assists are typically not recorded for penalty kicks, own goals, or some free kicks,
    /// depending on the competition's statistical recording rules.
    /// </remarks>
    public PlayerId? AssistId { get; set; }

    /// <summary>
    /// Returns a string representation of the goal including timing, scorer, and goal type information.
    /// </summary>
    /// <returns>A formatted string describing the goal event.</returns>
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

            switch (Type)
            {
                case GoalType.Regular when AssistId is not null:
                    _ = str.Append(CultureInfo.CurrentCulture, $" ({AssistId})");
                    break;
                case GoalType.Penalty:
                    _ = str.Append(" (p)");
                    break;
                case GoalType.Regular:
                case GoalType.OwnGoal:
                case GoalType.FreeKick:
                default:
                    break;
            }
        }

        return str.ToString();
    }
}
