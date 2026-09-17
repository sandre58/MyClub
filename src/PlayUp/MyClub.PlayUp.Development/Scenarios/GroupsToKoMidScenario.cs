// -----------------------------------------------------------------------
// <copyright file="GroupsToKoMidScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Multi-phase mid: groups finished, Qual → QF population + Slot Draw, KO Draft.
/// </summary>
public sealed class GroupsToKoMidScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "groups-to-ko-mid";

    /// <inheritdoc />
    public string Name => "Groups → KO mid";

    /// <inheritdoc />
    public string Description =>
        "Groups 2×4 finished → Top2 → QF population → Slot Draw — KO Draft (Qual V2 mid-state).";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Operational;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildGroupsToKoMidAsync(context, cancellationToken);
}
