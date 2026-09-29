// -----------------------------------------------------------------------
// <copyright file="SwissReadyScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Swiss 8×3 Prepared only — Ready, 0 rounds yet (also Regulation Swiss schematic).
/// </summary>
public sealed class SwissReadyScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "swiss-ready";

    /// <inheritdoc />
    public string Name => "Swiss Ready";

    /// <inheritdoc />
    public string Description =>
        "Swiss 8×3 — Ready (no GenerateNextRound yet). Règlement Swiss: ReplaceRegulation via Ready→Draft confirm.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe { get; } = new()
    {
        DisplayName = "Suisse Ready",
        Format = RecipeFormat.Swiss,
        TeamCount = 8,
        SwissRoundCount = 3,
        StageName = "Suisse"
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildStructuredAsync(
            context,
            Recipe!,
            StructuredSeedLifecycle.Ready,
            cancellationToken);
}
