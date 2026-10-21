// -----------------------------------------------------------------------
// <copyright file="InvariantsNumOverExtractionScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Invariants QA — NUM-002a OverExtraction (Qual EachGroup 1–3 from capacity 2×2).
/// </summary>
public sealed class InvariantsNumOverExtractionScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "invariants-num-overextraction";

    /// <inheritdoc />
    public string Name => "Invariants — OverExtraction";

    /// <inheritdoc />
    public string Description =>
        "Poules 2×2 + Qual EachGroup Top 1–3 → Population — OverExtraction (Prepare Gate, ≠ StructureGraphInvalid).";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildInvariantsNumOverExtractionAsync(context, cancellationToken);
}
