// -----------------------------------------------------------------------
// <copyright file="RegulationSwissTemplate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Templates;

/// <summary>
/// Lightweight Swiss Draft seed for Règlement hub schematic QA.
/// </summary>
public sealed class RegulationSwissTemplate : ICompetitionTemplate
{
    /// <inheritdoc />
    public string Id => "regulation-swiss";

    /// <inheritdoc />
    public string Name => "Démo Suisse";

    /// <inheritdoc />
    public string Description =>
        "Suisse 8×3 · Draft (structure only) — schéma hub Règlement. :progress ignored.";

    /// <inheritdoc />
    public CompetitionRecipe Recipe { get; } = new()
    {
        DisplayName = "Démo Suisse",
        Format = RecipeFormat.Swiss,
        TeamCount = 8,
        SwissRoundCount = 3,
        StageName = "Suisse",
        TeamNames = TeamNameSource.Generated
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildRegulationSwissDemoAsync(context, cancellationToken);
}
