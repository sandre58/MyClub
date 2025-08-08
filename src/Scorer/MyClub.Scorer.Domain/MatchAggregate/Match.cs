// -----------------------------------------------------------------------
// <copyright file="Match.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using MyClub.Scorer.Domain.CompetitionAggregate.Configurations;
using MyClub.Scorer.Domain.MatchAggregate.MatchEvents;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Matches;
using MyClub.Shared.Domain.Stadiums;
using MyClub.Shared.Domain.Teams;
using MyClub.Shared.Kernel.Primitives;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;
using MyNet.Utilities.Units;

namespace MyClub.Scorer.Domain.MatchAggregate;

/// <summary>
/// Represents a football match aggregate root that manages all aspects of a sporting encounter between two teams.
/// This aggregate handles match lifecycle, scoring, timing, and all events that occur during a match.
/// </summary>
public sealed class Match : AuditableEntity<MatchId>, IMatch, IAggregateRoot
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    private Match()
    {
        Home = null!;
        Away = null!;
        Format = null!;
        Rules = null!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Match"/> class with the specified parameters.
    /// </summary>
    /// <param name="id">The unique identifier for the match.</param>
    /// <param name="date">The scheduled date and time for the match.</param>
    /// <param name="homeTeam">The reference to the home team.</param>
    /// <param name="awayTeam">The reference to the away team.</param>
    /// <param name="matchFormat">The format configuration for this match.</param>
    /// <param name="matchRules">The rules configuration for this match.</param>
    private Match(MatchId id, DateTime date, TeamReference homeTeam, TeamReference awayTeam, MatchFormat matchFormat, MatchRules matchRules)
        : base(id)
    {
        Format = matchFormat;
        Rules = matchRules;
        OriginDate = date;

        Home = new(homeTeam);
        Away = new(awayTeam);
    }

    /// <summary>
    /// Creates a new match with the specified parameters.
    /// </summary>
    /// <param name="date">The scheduled date and time for the match.</param>
    /// <param name="homeTeam">The reference to the home team.</param>
    /// <param name="awayTeam">The reference to the away team.</param>
    /// <param name="matchFormat">The format configuration for this match. Uses default if not specified.</param>
    /// <param name="matchRules">The rules configuration for this match. Uses default if not specified.</param>
    /// <returns>A new <see cref="Match"/> instance.</returns>
    public static Match Create(DateTime date, TeamReference homeTeam, TeamReference awayTeam, MatchFormat? matchFormat = null, MatchRules? matchRules = null)
        => new(MatchId.New(), date, homeTeam, awayTeam, matchFormat ?? MatchFormat.Default, matchRules ?? MatchRules.Default);

    /// <summary>
    /// Gets the format configuration for this match, including timing and overtime rules.
    /// </summary>
    public MatchFormat Format { get; }

    /// <summary>
    /// Gets the rules configuration for this match, including allowed cards and other regulations.
    /// </summary>
    public MatchRules Rules { get; }

    /// <summary>
    /// Gets or sets the original scheduled date and time for the match.
    /// </summary>
    public DateTime OriginDate { get; set; }

    /// <summary>
    /// Gets or sets the postponed date and time if the match has been rescheduled.
    /// </summary>
    public DateTime? PostponedDate { get; set; }

    /// <summary>
    /// Gets the effective date and time for the match.
    /// Returns the postponed date if the match was rescheduled, otherwise returns the original date.
    /// </summary>
    public DateTime Date => PostponedDate ?? OriginDate;

    /// <summary>
    /// Gets the current status of the match.
    /// </summary>
    public MatchStatus Status { get; private set; }

    /// <summary>
    /// Gets the home team opponent information including score and match events.
    /// </summary>
    public MatchOpponent Home { get; private set; }

    /// <summary>
    /// Gets the away team opponent information including score and match events.
    /// </summary>
    public MatchOpponent Away { get; private set; }

    /// <summary>
    /// Gets the reference to the home team.
    /// </summary>
    public TeamReference HomeTeamReference => Home.Team;

    /// <summary>
    /// Gets the reference to the away team.
    /// </summary>
    public TeamReference AwayTeamReference => Away.Team;

    /// <summary>
    /// Gets or sets a value indicating whether the match is played at a neutral stadium.
    /// </summary>
    public bool IsNeutralStadium { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the stadium where the match is played.
    /// </summary>
    public StadiumId? StadiumId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the match result was decided after extra time.
    /// </summary>
    public bool AfterExtraTime { get; set; }

    /// <summary>
    /// Determines whether extra time should be used based on the match format and current score.
    /// </summary>
    /// <returns>True if extra time should be played; otherwise, false.</returns>
    public bool UseExtraTime() => Format.ExtraTimeIsEnabled && IsDraw();

    /// <summary>
    /// Determines whether penalty shootout should be used based on the match format and current score.
    /// </summary>
    /// <returns>True if penalty shootout should be played; otherwise, false.</returns>
    public bool UseShootout() => Format.ShootoutIsEnabled && IsDraw();

    /// <summary>
    /// Schedules the match for a specific date and time.
    /// </summary>
    /// <param name="date">The new date and time for the match.</param>
    public void Schedule(DateTime date)
    {
        if (Status == MatchStatus.Postponed && PostponedDate.HasValue)
            PostponedDate = date;
        else
            OriginDate = date;
    }

    /// <summary>
    /// Schedules the match by adding or subtracting a time offset from the current date.
    /// </summary>
    /// <param name="offset">The time offset value.</param>
    /// <param name="timeUnit">The unit of time for the offset.</param>
    public void Schedule(int offset, TimeUnit timeUnit)
    {
        if (offset == 0) return;

        if (Status == MatchStatus.Postponed && PostponedDate.HasValue)
            PostponedDate = PostponedDate.Value.AddFluentTimeSpan(offset.Unit(timeUnit));
        else
            OriginDate = OriginDate.AddFluentTimeSpan(offset.Unit(timeUnit));
    }

    /// <summary>
    /// Resets the match to its initial state (not started).
    /// </summary>
    public void Reset() => Reset(MatchStatus.None);

    /// <summary>
    /// Cancels the match and resets all scores and events.
    /// </summary>
    public void Cancel() => Reset(MatchStatus.Cancelled);

    /// <summary>
    /// Postpones the match to a future date.
    /// </summary>
    /// <param name="date">The new date for the postponed match. If null, the match is postponed without a specific new date.</param>
    public void Postpone(DateTime? date = null)
    {
        Reset(MatchStatus.Postponed);
        PostponedDate = date;
    }

    /// <summary>
    /// Starts the match, changing its status to in progress.
    /// </summary>
    public void Start() => Status = MatchStatus.InProgress;

    /// <summary>
    /// Suspends the match temporarily.
    /// </summary>
    public void Suspend() => Status = MatchStatus.Suspended;

    /// <summary>
    /// Marks the match as played and completed.
    /// </summary>
    public void Played() => Status = MatchStatus.Played;

    /// <summary>
    /// Resets the match state and sets the specified status.
    /// </summary>
    /// <param name="status">The new status for the match.</param>
    private void Reset(MatchStatus status)
    {
        Home.Reset();
        Away.Reset();
        AfterExtraTime = false;
        Status = status;
    }

    /// <summary>
    /// Gets the time period during which the match is scheduled to be played.
    /// </summary>
    /// <returns>A <see cref="Period"/> representing the match duration.</returns>
    public Period GetPeriod() => new(Date, Date.AddFluentTimeSpan(Format.GetFullTime()));

    /// <summary>
    /// Inverts the home and away teams, swapping their positions.
    /// </summary>
    public void Invert() => (Home, Away) = (Away, Home);

    /// <summary>
    /// Determines whether the match has a result (score has been set).
    /// </summary>
    /// <returns>True if the match has been played, is in progress, or is suspended; otherwise, false.</returns>
    public bool HasResult() => Status is MatchStatus.Played or MatchStatus.InProgress or MatchStatus.Suspended;

    /// <summary>
    /// Determines whether the match has a result for the specified team.
    /// </summary>
    /// <param name="team">The team to check.</param>
    /// <returns>True if the match has a result and the team participates; otherwise, false.</returns>
    public bool HasResult(TeamReference team) => HasResult() && Participate(team);

    /// <summary>
    /// Determines whether the match has been completed.
    /// </summary>
    /// <returns>True if the match status is Played; otherwise, false.</returns>
    public bool IsPlayed() => Status == MatchStatus.Played;

    /// <summary>
    /// Determines whether the match ended in a draw.
    /// </summary>
    /// <returns>True if both teams have the same score; otherwise, false.</returns>
    public bool IsDraw() => Home.Score == Away.Score;

    /// <summary>
    /// Gets the result type for the specified team.
    /// </summary>
    /// <param name="team">The team to get the result for.</param>
    /// <returns>The <see cref="MatchResultType"/> for the specified team.</returns>
    public MatchResultType GetResultOf(TeamReference team)
        => !HasResult(team)
            ? MatchResultType.None
            : GetOpponent(team)!.IsWithdrawn
                ? MatchResultType.Withdraw
                : GetOpponentAgainst(team)!.IsWithdrawn
                    ? MatchResultType.Win
                    : GetResultTypeOf(team);

    /// <summary>
    /// Gets the outcome for the specified team (Win, Draw, Loss, or None).
    /// </summary>
    /// <param name="team">The team to get the outcome for.</param>
    /// <returns>The <see cref="MatchOutcome"/> for the specified team.</returns>
    public MatchOutcome GetOutcomeOf(TeamReference team)
        => GetResultOf(team) switch
        {
            MatchResultType.Win or MatchResultType.WinAfterShootouts => MatchOutcome.Win,
            MatchResultType.Draw => MatchOutcome.Draw,
            MatchResultType.Loss or MatchResultType.Withdraw or MatchResultType.LossAfterShootouts => MatchOutcome.Loss,
            MatchResultType.None => MatchOutcome.None,
            _ => throw new InvalidOperationException()
        };

    /// <summary>
    /// Gets the detailed result type for the specified team, considering shootouts.
    /// </summary>
    /// <param name="team">The team to get the result for.</param>
    /// <param name="withShootout">Whether to consider penalty shootout results.</param>
    /// <returns>The detailed <see cref="MatchResultType"/> for the specified team.</returns>
    private MatchResultType GetResultTypeOf(TeamReference team, bool withShootout = true)
        => !HasResult(team)
            ? MatchResultType.None
            : GoalsFor(team) > GoalsAgainst(team) ? MatchResultType.Win
                : GoalsAgainst(team) > GoalsFor(team) ? MatchResultType.Loss
                    : Format.ShootoutIsEnabled && withShootout ? GetShootoutResultOf(team) : MatchResultType.Draw;

    /// <summary>
    /// Gets the penalty shootout result for the specified team.
    /// </summary>
    /// <param name="team">The team to get the shootout result for.</param>
    /// <returns>The shootout <see cref="MatchResultType"/> for the specified team.</returns>
    private MatchResultType GetShootoutResultOf(TeamReference team)
        => !HasResult(team)
            ? MatchResultType.None
            : ShootoutFor(team) > ShootoutAgainst(team) ? MatchResultType.WinAfterShootouts
                : ShootoutAgainst(team) > ShootoutFor(team) ? MatchResultType.LossAfterShootouts
                    : MatchResultType.Draw;

    /// <summary>
    /// Gets the winning team of the match.
    /// </summary>
    /// <returns>The <see cref="TeamReference"/> of the winning team, or null if the match was a draw or has no result.</returns>
    public TeamReference? GetWinner()
        => GetOutcomeOf(Home.Team) switch
        {
            MatchOutcome.Win => Home.Team,
            MatchOutcome.Loss => Away.Team,
            MatchOutcome.None => null,
            MatchOutcome.Draw => null,
            _ => throw new InvalidOperationException()
        };

    /// <summary>
    /// Gets the losing team of the match.
    /// </summary>
    /// <returns>The <see cref="TeamReference"/> of the losing team, or null if the match was a draw or has no result.</returns>
    public TeamReference? GetLooser()
        => GetOutcomeOf(Home.Team) switch
        {
            MatchOutcome.Win => Away.Team,
            MatchOutcome.Loss => Home.Team,
            MatchOutcome.None => null,
            MatchOutcome.Draw => null,
            _ => throw new InvalidOperationException()
        };

    /// <summary>
    /// Determines whether the specified team won the match.
    /// </summary>
    /// <param name="team">The team to check.</param>
    /// <returns>True if the team won; otherwise, false.</returns>
    public bool IsWonBy(TeamReference team) => GetOutcomeOf(team) == MatchOutcome.Win;

    /// <summary>
    /// Determines whether the specified team lost the match.
    /// </summary>
    /// <param name="team">The team to check.</param>
    /// <returns>True if the team lost; otherwise, false.</returns>
    public bool IsLostBy(TeamReference team) => GetOutcomeOf(team) == MatchOutcome.Loss;

    /// <summary>
    /// Determines whether the specified team withdrew from the match.
    /// </summary>
    /// <param name="team">The team to check.</param>
    /// <returns>True if the team withdrew; otherwise, false.</returns>
    public bool IsWithdrawn(TeamReference team) => GetOpponent(team)?.IsWithdrawn ?? false;

    /// <summary>
    /// Gets the number of goals scored by the specified team.
    /// </summary>
    /// <param name="team">The team to get goals for.</param>
    /// <returns>The number of goals scored by the team.</returns>
    public int GoalsFor(TeamReference team) => GetOpponent(team)?.Score ?? 0;

    /// <summary>
    /// Gets the number of goals scored against the specified team.
    /// </summary>
    /// <param name="team">The team to get goals against.</param>
    /// <returns>The number of goals scored against the team.</returns>
    public int GoalsAgainst(TeamReference team) => GetOpponentAgainst(team)?.Score ?? 0;

    /// <summary>
    /// Gets the number of penalty shootout goals scored by the specified team.
    /// </summary>
    /// <param name="team">The team to get shootout goals for.</param>
    /// <returns>The number of penalty shootout goals scored by the team.</returns>
    public int ShootoutFor(TeamReference team) => GetOpponent(team)?.GetShootoutScore() ?? 0;

    /// <summary>
    /// Gets the number of penalty shootout goals scored against the specified team.
    /// </summary>
    /// <param name="team">The team to get shootout goals against.</param>
    /// <returns>The number of penalty shootout goals scored against the team.</returns>
    public int ShootoutAgainst(TeamReference team) => GetOpponentAgainst(team)?.GetShootoutScore() ?? 0;

    /// <summary>
    /// Determines whether the specified team participates in this match.
    /// </summary>
    /// <param name="team">The team to check.</param>
    /// <returns>True if the team participates in the match; otherwise, false.</returns>
    public bool Participate(TeamReference team) => GetTeams().Contains(team);

    /// <summary>
    /// Determines whether the specified team is the home team.
    /// </summary>
    /// <param name="team">The team to check.</param>
    /// <returns>True if the team is the home team; otherwise, false.</returns>
    public bool IsHomeTeam(TeamReference team) => team == Home.Team;

    /// <summary>
    /// Determines whether the specified team is the away team.
    /// </summary>
    /// <param name="team">The team to check.</param>
    /// <returns>True if the team is the away team; otherwise, false.</returns>
    public bool IsAwayTeam(TeamReference team) => team == Away.Team;

    /// <summary>
    /// Gets the match opponent information for the specified team.
    /// </summary>
    /// <param name="team">The team to get opponent information for.</param>
    /// <returns>The <see cref="MatchOpponent"/> for the specified team, or null if the team doesn't participate.</returns>
    public MatchOpponent? GetOpponent(TeamReference team) => IsHomeTeam(team) ? Home : IsAwayTeam(team) ? Away : null;

    /// <summary>
    /// Gets the opposing team's match opponent information for the specified team.
    /// </summary>
    /// <param name="team">The team to get the opposing team's information for.</param>
    /// <returns>The <see cref="MatchOpponent"/> of the opposing team, or null if the team doesn't participate.</returns>
    public MatchOpponent? GetOpponentAgainst(TeamReference team) => IsHomeTeam(team) ? Away : IsAwayTeam(team) ? Home : null;

    /// <summary>
    /// Gets all teams participating in this match.
    /// </summary>
    /// <returns>A read-only collection of <see cref="TeamReference"/> representing both teams.</returns>
    public IReadOnlyCollection<TeamReference> GetTeams() => new List<TeamReference>
    {
        Home.Team, Away.Team
    }.AsReadOnly();

    /// <summary>
    /// Sets the match score using simple integer values.
    /// </summary>
    /// <param name="homeScore">The score for the home team.</param>
    /// <param name="awayScore">The score for the away team.</param>
    /// <param name="afterExtraTime">Whether the result was decided after extra time.</param>
    /// <param name="homeShootoutScore">The penalty shootout score for the home team.</param>
    /// <param name="awayShootoutScore">The penalty shootout score for the away team.</param>
    public void SetScore(int homeScore, int awayScore, bool afterExtraTime = false, int? homeShootoutScore = null, int? awayShootoutScore = null)
    {
        var hasWithdraw = Home.IsWithdrawn || Away.IsWithdrawn;

        AfterExtraTime = Format.ExtraTimeIsEnabled && !hasWithdraw && afterExtraTime;

        Home.SetScore(homeScore, Format.ShootoutIsEnabled && !hasWithdraw ? homeShootoutScore : null);
        Away.SetScore(awayScore, Format.ShootoutIsEnabled && !hasWithdraw ? awayShootoutScore : null);
    }

    /// <summary>
    /// Sets the match score using detailed goal and shootout collections.
    /// </summary>
    /// <param name="homeGoals">The goals scored by the home team.</param>
    /// <param name="awayGoals">The goals scored by the away team.</param>
    /// <param name="afterExtraTime">Whether the result was decided after extra time.</param>
    /// <param name="homeShootouts">The penalty shootout attempts by the home team.</param>
    /// <param name="awayShootouts">The penalty shootout attempts by the away team.</param>
    public void SetScore(IEnumerable<Goal> homeGoals, IEnumerable<Goal> awayGoals, bool afterExtraTime = false, IEnumerable<PenaltyShootout>? homeShootouts = null, IEnumerable<PenaltyShootout>? awayShootouts = null)
    {
        var hasWithdraw = Home.IsWithdrawn || Away.IsWithdrawn;

        AfterExtraTime = Format.ExtraTimeIsEnabled && !hasWithdraw && afterExtraTime;

        Home.SetScore(homeGoals, Format.ShootoutIsEnabled && !hasWithdraw ? homeShootouts : null);
        Away.SetScore(awayGoals, Format.ShootoutIsEnabled && !hasWithdraw ? awayShootouts : null);
    }

    /// <summary>
    /// Returns a string representation of the match including date, teams, and score.
    /// </summary>
    /// <returns>A formatted string describing the match.</returns>
    public override string ToString()
    {
        var str = new StringBuilder($"{Date:G} | {Home.Team} vs {Away.Team}");

        if (Status is not (MatchStatus.Played or MatchStatus.InProgress or MatchStatus.Suspended))
            return str.ToString();

        if (Home.IsWithdrawn)
        {
            _ = str.Append(CultureInfo.CurrentCulture, $"{Home.Team} is withdrawn");
        }
        else if (Away.IsWithdrawn)
        {
            _ = str.Append(CultureInfo.CurrentCulture, $"{Away.Team} is withdrawn");
        }
        else
        {
            _ = str.Append(CultureInfo.CurrentCulture, $" : {Home.Score}-{Away.Score}");

            if (Home.Score == Away.Score)
            {
                if (Format.ShootoutIsEnabled)
                {
                    _ = str.Append(CultureInfo.CurrentCulture, $" ({Home.GetShootoutScore()}-{Away.GetShootoutScore()})");
                }
            }
            else if (AfterExtraTime && Format.ExtraTimeIsEnabled)
            {
                _ = str.Append(" e");
            }
        }

        return str.ToString();
    }
}
