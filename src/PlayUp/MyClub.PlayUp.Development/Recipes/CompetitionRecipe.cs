// -----------------------------------------------------------------------
// <copyright file="CompetitionRecipe.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Development.Recipes;

/// <summary>
/// Declares competition structure only (not business state, scores, or Cockpit actions).
/// </summary>
public sealed record CompetitionRecipe
{
    /// <summary>Gets the competition display name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Gets the V1 structure format.</summary>
    public required RecipeFormat Format { get; init; }

    /// <summary>Gets the planned team count.</summary>
    public required int TeamCount { get; init; }

    /// <summary>Gets the optional primary stage name.</summary>
    public string? StageName { get; init; }

    /// <summary>Gets championship matchday count (Championship).</summary>
    public int? MatchdayCount { get; init; }

    /// <summary>Gets group count (Groups).</summary>
    public int? GroupCount { get; init; }

    /// <summary>Gets participants per group (Groups).</summary>
    public int? ParticipantsPerGroup { get; init; }

    /// <summary>Gets cup bracket size (Cup, power of two).</summary>
    public int? BracketSize { get; init; }

    /// <summary>Gets how team display names are sourced.</summary>
    public TeamNameSource TeamNames { get; init; } = TeamNameSource.Generated;

    /// <summary>Gets optional dataset competition key for real-inspired names.</summary>
    public string? DatasetCompetitionKey { get; init; }
}
