// -----------------------------------------------------------------------
// <copyright file="GroupsManualPlacementScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Manual group placement screen: empty pools, pot rules, and a Ready pool with a match.
/// </summary>
public sealed class GroupsManualPlacementScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "groups-manual-placement";

    /// <inheritdoc />
    public string Name => "Groups manual placement";

    /// <inheritdoc />
    public string Description =>
        "Two pools of four: empty manual grid, pot rules with an empty grid, and a Ready pool that already has a match.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildGroupsManualPlacementAsync(context, cancellationToken);
}
