// -----------------------------------------------------------------------
// <copyright file="GroupsDrawPendingScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Groups with pot DrawRules, empty groups — draw not applied (Draft).
/// </summary>
public sealed class GroupsDrawPendingScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "groups-draw-pending";

    /// <inheritdoc />
    public string Name => "Groups draw pending";

    /// <inheritdoc />
    public string Description =>
        "Groups 4×4 + pot DrawRules + full composition, groups empty — Draft awaiting draw (E4).";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildGroupsDrawPendingAsync(context, cancellationToken);
}
