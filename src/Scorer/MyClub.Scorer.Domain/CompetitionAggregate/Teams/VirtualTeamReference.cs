// -----------------------------------------------------------------------
// <copyright file="VirtualTeamReference.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Scorer.Domain.RoundAggregate;
using MyClub.Scorer.Domain.StageAggregate;
using MyClub.Shared.Domain.Teams;

namespace MyClub.Scorer.Domain.CompetitionAggregate.Teams;

/// <summary>
/// Abstract base class for virtual team references that represent teams not yet determined
/// but will be resolved based on competition results. Virtual team references are essential
/// for tournament bracket management and advance competition setup.
/// </summary>
public record VirtualTeamReference : TeamReference;

/// <summary>
/// Represents a virtual team reference that will be resolved based on the outcome of a specific fixture.
/// This allows tournaments to reference teams as "Winner of Match A" or "Loser of Match B"
/// before the actual matches are played.
/// </summary>
/// <param name="RoundId">The identifier of the round containing the fixture.</param>
/// <param name="FixtureId">The identifier of the specific fixture whose result determines the team.</param>
/// <param name="Type">The type indicating whether this refers to the winner or loser of the fixture.</param>
public record FixtureResultReference(RoundId RoundId, FixtureId FixtureId, VirtualTeamType Type) : VirtualTeamReference;

/// <summary>
/// Represents a virtual team reference that will be resolved based on the final ranking
/// of teams within a specific group. This enables tournaments to reference teams by their
/// group position (1st, 2nd, 3rd, etc.) before group stage completion.
/// </summary>
/// <param name="StageId">The identifier of the stage containing the group.</param>
/// <param name="GroupId">The identifier of the specific group whose ranking determines the team.</param>
/// <param name="Rank">The final position/rank within the group (1 = first place, 2 = second place, etc.).</param>
public record GroupRankReference(StageId StageId, GroupId GroupId, int Rank) : VirtualTeamReference;

/// <summary>
/// Represents a virtual team reference that will be resolved based on the final ranking
/// of teams within a championship stage. This allows tournaments to reference teams by their
/// final league position before the championship stage is completed.
/// </summary>
/// <param name="StageId">The identifier of the championship stage whose ranking determines the team.</param>
/// <param name="Rank">The final position/rank within the championship (1 = champion, 2 = runner-up, etc.).</param>
public record ChampionshipRankReference(StageId StageId, int Rank) : VirtualTeamReference;
