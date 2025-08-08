// -----------------------------------------------------------------------
// <copyright file="MatchOpponent.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MyClub.Scorer.Domain.CompetitionAggregate.Teams;
using MyClub.Scorer.Domain.MatchAggregate.MatchEvents;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Matches;
using MyClub.Shared.Domain.Teams;
using MyNet.Utilities;
using MyNet.Utilities.Helpers;

namespace MyClub.Scorer.Domain.MatchAggregate;

/// <summary>
/// Represents one of the two teams participating in a match, encapsulating all their match-specific data
/// including goals, cards, penalty shootouts, and match state. This class serves as the team's view
/// within the context of a specific match and manages all events and statistics for that team.
/// </summary>
/// <param name="team">The team reference representing which team this opponent represents.</param>
public class MatchOpponent(TeamReference team)
{
    private readonly List<Goal> _goals = [];
    private readonly List<Card> _cards = [];
    private readonly List<PenaltyShootout> _shootout = [];

    /// <summary>
    /// Gets the match identifier that this opponent belongs to.
    /// This property is used internally by Entity Framework Core to maintain the relationship.
    /// </summary>
    [UsedImplicitly(Reason = "Used by EF Core.")]
    internal MatchId MatchId { get; private set; } = null!;

    /// <summary>
    /// Gets the team reference representing which team this opponent represents in the match.
    /// This can be a concrete team or a virtual team reference resolved from other results.
    /// </summary>
    public TeamReference Team { get; } = team;

    /// <summary>
    /// Gets a value indicating whether this team has withdrawn from the match.
    /// A withdrawn team forfeits the match, typically resulting in a default loss.
    /// </summary>
    /// <remarks>
    /// Team withdrawal can occur due to various reasons:
    /// <list type="bullet">
    /// <item><description>Insufficient players to continue</description></item>
    /// <item><description>Disciplinary reasons (multiple red cards)</description></item>
    /// <item><description>Safety concerns or external factors</description></item>
    /// <item><description>Administrative decisions</description></item>
    /// </list>
    /// </remarks>
    public bool IsWithdrawn { get; private set; }

    /// <summary>
    /// Gets the read-only collection of goals scored by this team during the match.
    /// Goals are ordered chronologically by when they were scored.
    /// </summary>
    public IReadOnlyCollection<Goal> Goals => _goals.AsReadOnly();

    /// <summary>
    /// Gets the read-only collection of cards (yellow/red) received by this team's players during the match.
    /// Cards represent disciplinary actions taken by the referee.
    /// </summary>
    public IReadOnlyCollection<Card> Cards => _cards.AsReadOnly();

    /// <summary>
    /// Gets the read-only collection of penalty shootout attempts taken by this team.
    /// This is populated only when the match goes to a penalty shootout.
    /// </summary>
    public IReadOnlyCollection<PenaltyShootout> Shootout => _shootout.AsReadOnly();

    /// <summary>
    /// Gets the current score (number of goals) for this team in regular play.
    /// This does not include penalty shootout goals, which are tracked separately.
    /// </summary>
    public int Score => Goals.Count;

    /// <summary>
    /// Adds a regular goal scored by this team at the specified minute.
    /// This is a convenience method for the most common goal type.
    /// </summary>
    /// <param name="minute">The minute when the goal was scored. Can be null if timing is not specified.</param>
    /// <returns>The created Goal entity.</returns>
    public Goal AddGoal(int? minute = null) => AddGoal(Goal.Create(GoalType.Regular, minute: minute));

    /// <summary>
    /// Adds an existing goal entity to this team's goal collection.
    /// </summary>
    /// <param name="goal">The goal entity to add to this team's tally.</param>
    /// <returns>The added Goal entity.</returns>
    /// <remarks>
    /// This method allows for adding goals with specific types (own goal, penalty, etc.)
    /// and detailed information like scorer, minute, and assists.
    /// </remarks>
    public Goal AddGoal(Goal goal)
    {
        _goals.Add(goal);

        return goal;
    }

    /// <summary>
    /// Removes the most recently scored goal from this team's tally.
    /// This is useful for correcting scoring errors during live match updates.
    /// </summary>
    public void RemoveLastGoal()
    {
        var lastGoal = _goals.LastOrDefault();

        if (lastGoal is not null)
            _ = _goals.Remove(lastGoal);
    }

    /// <summary>
    /// Adds a penalty shootout attempt with the specified result and taker.
    /// This is a convenience method for recording penalty attempts during shootouts.
    /// </summary>
    /// <param name="result">The outcome of the penalty attempt (succeeded, failed, or pending).</param>
    /// <param name="takerId">The identifier of the player who took the penalty. Can be null if not specified.</param>
    /// <returns>The created PenaltyShootout entity.</returns>
    public PenaltyShootout AddPenaltyShootout(PenaltyShootoutOutcome result, PlayerId? takerId = null) => AddPenaltyShootout(PenaltyShootout.Create(takerId, result));

    /// <summary>
    /// Adds an existing penalty shootout entity to this team's shootout collection.
    /// </summary>
    /// <param name="penaltyShootout">The penalty shootout attempt to add.</param>
    /// <returns>The added PenaltyShootout entity.</returns>
    public PenaltyShootout AddPenaltyShootout(PenaltyShootout penaltyShootout)
    {
        _shootout.Add(penaltyShootout);

        return penaltyShootout;
    }

    /// <summary>
    /// Removes the most recent successful penalty shootout from this team's tally.
    /// This is useful for correcting penalty shootout errors during live updates.
    /// </summary>
    public void RemoveLastSucceededPenaltyShootout()
    {
        var lastPenaltyShootout = _shootout.LastOrDefault(static x => x.Result == PenaltyShootoutOutcome.Succeeded);

        if (lastPenaltyShootout is not null)
            _ = _shootout.Remove(lastPenaltyShootout);
    }

    /// <summary>
    /// Gets all match events (goals and cards) for this team ordered chronologically.
    /// This provides a unified view of all significant events involving this team during the match.
    /// </summary>
    /// <returns>An enumerable collection of match events ordered by minute.</returns>
    /// <remarks>
    /// The returned events include both goals and disciplinary cards, providing a complete
    /// timeline of this team's involvement in match incidents.
    /// </remarks>
    public IEnumerable<MatchEvent> GetEvents() => Goals.OfType<MatchEvent>().Union(Cards).OrderBy(static x => x.Minute);

    /// <summary>
    /// Sets the team's score to specific values, replacing any existing goals and shootout results.
    /// This method is useful for bulk score updates or match result corrections.
    /// </summary>
    /// <param name="score">The number of goals to set for this team in regular play.</param>
    /// <param name="shootoutScore">The number of successful penalties in a shootout. Can be null if no shootout occurred.</param>
    /// <remarks>
    /// This method clears existing goals and penalties before setting the new scores.
    /// Goals are created as generic regular goals without specific details.
    /// </remarks>
    public void SetScore(int score, int? shootoutScore = null)
    {
        ResetScore();
        EnumerableHelper.Iteration(score, _ => AddGoal());

        if (shootoutScore.HasValue)
            EnumerableHelper.Iteration(shootoutScore.Value, _ => AddPenaltyShootout(PenaltyShootout.Create(result: PenaltyShootoutOutcome.Succeeded)));
    }

    /// <summary>
    /// Sets the team's score using specific goal and penalty shootout entities.
    /// This method preserves detailed information about each goal and penalty.
    /// </summary>
    /// <param name="goals">The collection of goals to set for this team.</param>
    /// <param name="shootouts">The collection of penalty shootout attempts. Can be null if no shootout occurred.</param>
    /// <remarks>
    /// This method is useful when detailed goal information (scorer, minute, type) needs to be preserved
    /// while updating the team's match record.
    /// </remarks>
    public void SetScore(IEnumerable<Goal> goals, IEnumerable<PenaltyShootout>? shootouts = null)
    {
        ResetScore();
        goals.ForEach(x => AddGoal(x));
        shootouts?.ForEach(x => AddPenaltyShootout(x));
    }

    /// <summary>
    /// Gets the penalty shootout score (number of successful penalties) for this team.
    /// </summary>
    /// <returns>The number of penalties successfully converted by this team in the shootout.</returns>
    public int GetShootoutScore() => Shootout.Count(static x => x.Result == PenaltyShootoutOutcome.Succeeded);

    /// <summary>
    /// Adds a disciplinary card to this team's record for the match.
    /// </summary>
    /// <param name="card">The card (yellow or red) to add to this team's disciplinary record.</param>
    /// <returns>The added Card entity.</returns>
    public Card AddCard(Card card)
    {
        _cards.Add(card);

        return card;
    }

    /// <summary>
    /// Marks this team as withdrawn from the match, clearing all match data.
    /// A withdrawn team typically forfeits the match.
    /// </summary>
    public void DoWithdraw() => Reset(true);

    /// <summary>
    /// Resets all match data for this team without marking as withdrawn.
    /// This is useful for clearing match data while keeping the team active.
    /// </summary>
    public void Reset() => Reset(false);

    /// <summary>
    /// Resets the team's match state and data.
    /// </summary>
    /// <param name="isWithdrawn">Whether to mark the team as withdrawn from the match.</param>
    private void Reset(bool isWithdrawn)
    {
        IsWithdrawn = isWithdrawn;
        ResetScore();
        _cards.Clear();
    }

    /// <summary>
    /// Clears all goals and penalty shootout data for this team.
    /// </summary>
    private void ResetScore()
    {
        _shootout.Clear();
        _goals.Clear();
    }

    /// <summary>
    /// Returns a string representation of this match opponent using the team's name.
    /// </summary>
    /// <returns>The string representation of the team.</returns>
    public override string? ToString() => Team.ToString();
}
