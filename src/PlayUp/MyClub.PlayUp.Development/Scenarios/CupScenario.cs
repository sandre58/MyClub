// -----------------------------------------------------------------------
// <copyright file="CupScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Cup of 16 (single principal round) with selectable <see cref="SeedProgress"/>.
/// </summary>
public sealed class CupScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "cup";

    /// <inheritdoc />
    public string Name => "Cup";

    /// <inheritdoc />
    public string Description => "Cup 16 — single principal knockout round. Use :prepared|:running|:finished.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Operational;

    /// <inheritdoc />
    public bool AcceptsProgress => true;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe { get; } = new()
    {
        DisplayName = "Coupe — tour principal",
        Format = RecipeFormat.Cup,
        TeamCount = 16,
        BracketSize = 16,
        StageName = "Tour à élimination",
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildStructuredAsync(context, Recipe!, cancellationToken);
}
