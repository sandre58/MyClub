// -----------------------------------------------------------------------
// <copyright file="InvariantsFlowRoutingScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Invariants QA — FLOW-004 RoutingNondeterminism (duplicate Qual source → two Population destinations).
/// </summary>
public sealed class InvariantsFlowRoutingScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "invariants-flow-routing";

    /// <inheritdoc />
    public string Name => "Invariants — RoutingNondeterminism";

    /// <inheritdoc />
    public string Description =>
        "Poules Overall P1 → Dest A et Dest B (même source) — RoutingNondeterminism (Prepare Gate). Hors FEED WhoFeeds.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildInvariantsFlowRoutingAsync(context, cancellationToken);
}
