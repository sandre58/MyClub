// -----------------------------------------------------------------------
// <copyright file="Ligue1Template.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Templates;

/// <summary>
/// Ligue 1 inspired championship (18 clubs, single round-robin — not double RR).
/// </summary>
public sealed class Ligue1Template : ICompetitionTemplate
{
    /// <inheritdoc />
    public string Id => "ligue-1";

    /// <inheritdoc />
    public string Name => "Ligue 1";

    /// <inheritdoc />
    public string Description =>
        "Championship · 18 clubs (JSON). Domain V1: single RR only (not 34-matchday double RR).";

    /// <inheritdoc />
    public CompetitionRecipe Recipe { get; } = new()
    {
        DisplayName = "Ligue 1",
        Format = RecipeFormat.Championship,
        TeamCount = 18,
        MatchdayCount = 17,
        StageName = "Championnat",
        TeamNames = TeamNameSource.Dataset,
        DatasetCompetitionKey = "ligue-1"
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildStructuredAsync(context, Recipe, cancellationToken);
}
