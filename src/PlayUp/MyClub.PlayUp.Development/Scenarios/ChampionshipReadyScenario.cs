// -----------------------------------------------------------------------
// <copyright file="ChampionshipReadyScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Championship materialized and Prepared — competition/stage Ready (not started).
/// </summary>
public sealed class ChampionshipReadyScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "championship-ready";

    /// <inheritdoc />
    public string Name => "Championship Ready";

    /// <inheritdoc />
    public string Description => "8-team championship — Structure Ready (matches Scheduled, not started).";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe { get; } = new()
    {
        DisplayName = "Championnat Ready",
        Format = RecipeFormat.Championship,
        TeamCount = 8,
        StageName = "Championnat"
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildStructuredAsync(
            context,
            Recipe!,
            StructuredSeedLifecycle.Ready,
            cancellationToken);
}
