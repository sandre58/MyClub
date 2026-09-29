// -----------------------------------------------------------------------
// <copyright file="Swiss8X3Scenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Swiss classique, 8 teams × 3 rounds (progressive GenerateNextRound).
/// </summary>
public sealed class Swiss8X3Scenario : IScenario
{
    /// <inheritdoc />
    public string Id => "swiss-8x3";

    /// <inheritdoc />
    public string Name => "Swiss 8×3";

    /// <inheritdoc />
    public string Description =>
        "Swiss classique (8 teams, 3 rounds). Use :prepared|:running|:finished. Matchdays via GenerateNextRound.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Operational;

    /// <inheritdoc />
    public bool AcceptsProgress => true;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe { get; } = new()
    {
        DisplayName = "Swiss 8×3",
        Format = RecipeFormat.Swiss,
        TeamCount = 8,
        SwissRoundCount = 3,
        StageName = "Swiss"
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildStructuredAsync(context, Recipe!, cancellationToken: cancellationToken);
}
