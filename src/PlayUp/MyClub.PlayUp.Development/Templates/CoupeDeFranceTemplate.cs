// -----------------------------------------------------------------------
// <copyright file="CoupeDeFranceTemplate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Templates;

/// <summary>
/// Coupe de France inspired cup of 32 — single principal round only.
/// </summary>
public sealed class CoupeDeFranceTemplate : ICompetitionTemplate
{
    /// <inheritdoc />
    public string Id => "coupe-de-france";

    /// <inheritdoc />
    public string Name => "Coupe de France";

    /// <inheritdoc />
    public string Description =>
        "Cup 32 (JSON). Domain V1: single principal knockout round (not full bracket tree).";

    /// <inheritdoc />
    public CompetitionRecipe Recipe { get; } = new()
    {
        DisplayName = "Coupe de France",
        Format = RecipeFormat.Cup,
        TeamCount = 32,
        BracketSize = 32,
        StageName = "Tour à élimination",
        TeamNames = TeamNameSource.Dataset,
        DatasetCompetitionKey = "coupe-de-france",
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildStructuredAsync(context, Recipe, cancellationToken);
}
