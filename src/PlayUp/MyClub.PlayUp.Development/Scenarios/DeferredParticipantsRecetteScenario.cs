// -----------------------------------------------------------------------
// <copyright file="DeferredParticipantsRecetteScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// RECETTE T5 competitions: a cup match can exist before both sides are known, then Apply fills them.
/// </summary>
public sealed class DeferredParticipantsRecetteScenario : IScenario
{
    /// <inheritdoc />
    public string Id => DeferredParticipantsRecetteNames.ScenarioId;

    /// <inheritdoc />
    public string Name => "RECETTE T5 — Participants différés";

    /// <inheritdoc />
    public string Description =>
        "Semi-final planned before resolution, one side then the other filled by Apply, and a group standing that ignores the deferred cup match.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Operational;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public Recipes.CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildDeferredParticipantsRecetteAsync(context, cancellationToken);
}
