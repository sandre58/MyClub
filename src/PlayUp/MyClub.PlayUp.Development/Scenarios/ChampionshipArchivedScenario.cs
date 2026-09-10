// -----------------------------------------------------------------------
// <copyright file="ChampionshipArchivedScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Championship finished then Archived.
/// </summary>
public sealed class ChampionshipArchivedScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "championship-archived";

    /// <inheritdoc />
    public string Name => "Championship Archived";

    /// <inheritdoc />
    public string Description => "8-team championship Completed then Archived.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Terminal;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe { get; } = new()
    {
        DisplayName = "Championnat Archived",
        Format = RecipeFormat.Championship,
        TeamCount = 8,
        MatchdayCount = 7,
        StageName = "Championnat"
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildStructuredAsync(
            context,
            Recipe!,
            StructuredSeedLifecycle.Archived,
            cancellationToken);
}
