// -----------------------------------------------------------------------
// <copyright file="WorldCupTemplate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Templates;

/// <summary>
/// World Cup inspired group stage only (8×4) — no knockout pipeline.
/// </summary>
public sealed class WorldCupTemplate : ICompetitionTemplate
{
    /// <inheritdoc />
    public string Id => "world-cup";

    /// <inheritdoc />
    public string Name => "FIFA World Cup";

    /// <inheritdoc />
    public string Description =>
        "Groups 8×4 · 32 nations (JSON). Domain V1: group stage only (no KO tree).";

    /// <inheritdoc />
    public CompetitionRecipe Recipe { get; } = new()
    {
        DisplayName = "FIFA World Cup",
        Format = RecipeFormat.Groups,
        TeamCount = 32,
        GroupCount = 8,
        ParticipantsPerGroup = 4,
        StageName = "Phase de groupes",
        TeamNames = TeamNameSource.Dataset,
        DatasetCompetitionKey = "world-cup",
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildStructuredAsync(context, Recipe, cancellationToken);
}
