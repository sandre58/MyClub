// -----------------------------------------------------------------------
// <copyright file="CupCompositionPartialScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Cup 16 with partial root composition (10/16) — Structure Entrées E1.
/// </summary>
public sealed class CupCompositionPartialScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "cup-composition-partial";

    /// <inheritdoc />
    public string Name => "Cup composition partial";

    /// <inheritdoc />
    public string Description =>
        "Cup 16 bracket, composition 10/16 (E1 Completer). Contrast: cup-draw-pending = E0, cup-composition-complete = E2.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildCupCompositionPartialAsync(context, cancellationToken);
}
