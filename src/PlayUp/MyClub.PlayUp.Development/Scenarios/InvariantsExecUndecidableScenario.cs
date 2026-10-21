// -----------------------------------------------------------------------
// <copyright file="InvariantsExecUndecidableScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Invariants QA — EXEC-001 ConfrontationUndecidable (Prepare Gate) without TAB.
/// </summary>
public sealed class InvariantsExecUndecidableScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "invariants-exec-undecidable";

    /// <inheritdoc />
    public string Name => "Invariants — ConfrontationUndecidable";

    /// <inheritdoc />
    public string Description =>
        "Demi → Finale Places avec Progression nécessaire sans TAB — ConfrontationUndecidable (Prepare Gate). Draft persistable.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildInvariantsExecUndecidableAsync(context, cancellationToken);
}
