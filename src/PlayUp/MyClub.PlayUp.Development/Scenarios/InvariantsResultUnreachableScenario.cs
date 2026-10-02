// -----------------------------------------------------------------------
// <copyright file="InvariantsResultUnreachableScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Invariants QA — RESULT AuthoredAwardUnreachable / ExpectedRankUnreachable (hors Ready).
/// </summary>
public sealed class InvariantsResultUnreachableScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "invariants-result-unreachable";

    /// <inheritdoc />
    public string Name => "Invariants — AuthoredAwardUnreachable";

    /// <inheritdoc />
    public string Description =>
        "Finale + Attribution sur confrontation absente — AuthoredAwardUnreachable / ExpectedRankUnreachable (Signals hors Ready).";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildInvariantsResultUnreachableAsync(context, cancellationToken);
}
