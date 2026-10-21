// -----------------------------------------------------------------------
// <copyright file="GroupsScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Groups 4×4 with selectable <see cref="SeedProgress"/>.
/// </summary>
public sealed class GroupsScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "groups";

    /// <inheritdoc />
    public string Name => "Groups";

    /// <inheritdoc />
    public string Description => "Four groups of four. Use :prepared|:running|:finished.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Operational;

    /// <inheritdoc />
    public bool AcceptsProgress => true;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe { get; } = new()
    {
        DisplayName = "Phase de groupes",
        Format = RecipeFormat.Groups,
        TeamCount = 16,
        GroupCount = 4,
        PlacesPerGroup = 4,
        StageName = "Phase de groupes"
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildStructuredAsync(context, Recipe!, cancellationToken: cancellationToken);
}
