// -----------------------------------------------------------------------
// <copyright file="CupCompositionCompleteScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Cup 16 with full root composition (16/16) — Structure Entrées E2 + tirage pending.
/// </summary>
public sealed class CupCompositionCompleteScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "cup-composition-complete";

    /// <inheritdoc />
    public string Name => "Cup composition complete";

    /// <inheritdoc />
    public string Description =>
        "Cup 16 bracket, composition 16/16 (E2 Modifier), no pairing draw yet.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildCupCompositionCompleteAsync(context, cancellationToken);
}
