// -----------------------------------------------------------------------
// <copyright file="QualAutoPlaceMidScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Case 2: groups finished, Qual Auto Place dual-writes QF Places (no Slot Draw).
/// </summary>
public sealed class QualAutoPlaceMidScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "qual-auto-place-mid";

    /// <inheritdoc />
    public string Name => "Qual Auto Place mid";

    /// <inheritdoc />
    public string Description =>
        "Groups 2×4 finished → Top2 Qual Auto Place → QF slots (dual-write Population+Place) — KO Draft, no Draw. WhoFeeds = Qual.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Operational;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildQualAutoPlaceMidAsync(context, cancellationToken);
}
