// -----------------------------------------------------------------------
// <copyright file="CupQfSfScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Multi-stage Cup capacity demo: QF finished → SF slots occupied (Overview from-slots).
/// </summary>
public sealed class CupQfSfScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "cup-qf-sf";

    /// <inheritdoc />
    public string Name => "Cup QF → SF";

    /// <inheritdoc />
    public string Description =>
        "8 teams · QF DrawRules + Slot draw applied · Prog Auto Place winners → SF slots (WhoFeeds) — materialize-from-slots left for Overview/Stage UI.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Operational;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildCupQfSfAsync(context, cancellationToken);
}
