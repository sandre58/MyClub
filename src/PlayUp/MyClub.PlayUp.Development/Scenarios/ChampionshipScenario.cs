// -----------------------------------------------------------------------
// <copyright file="ChampionshipScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Generated championship (8 teams) with selectable <see cref="SeedProgress"/>.
/// </summary>
public sealed class ChampionshipScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "championship";

    /// <inheritdoc />
    public string Name => "Championship";

    /// <inheritdoc />
    public string Description => "Generated championship (8 teams). Use :prepared|:running|:finished.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Operational;

    /// <inheritdoc />
    public bool AcceptsProgress => true;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe { get; } = new()
    {
        DisplayName = "Championnat généré",
        Format = RecipeFormat.Championship,
        TeamCount = 8,
        StageName = "Championnat"
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildStructuredAsync(context, Recipe!, cancellationToken: cancellationToken);
}
