// -----------------------------------------------------------------------
// <copyright file="GroupsSuspendedScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Groups mid-run then Suspended (competition + stage).
/// </summary>
public sealed class GroupsSuspendedScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "groups-suspended";

    /// <inheritdoc />
    public string Name => "Groups Suspended";

    /// <inheritdoc />
    public string Description => "Groups 4×4 mid-results, then Suspended.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Operational;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe { get; } = new()
    {
        DisplayName = "Groupes Suspended",
        Format = RecipeFormat.Groups,
        TeamCount = 16,
        GroupCount = 4,
        PlacesPerGroup = 4,
        StageName = "Phase de groupes"
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildStructuredAsync(
            context,
            Recipe!,
            StructuredSeedLifecycle.Suspended,
            cancellationToken);
}
