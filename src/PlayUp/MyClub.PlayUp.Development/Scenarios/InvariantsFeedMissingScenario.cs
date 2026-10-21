// -----------------------------------------------------------------------
// <copyright file="InvariantsFeedMissingScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Invariants QA — SlotFeedMissing (Incomplete Signal) + PopulationShortfall on empty KO Places.
/// </summary>
public sealed class InvariantsFeedMissingScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "invariants-feed-missing";

    /// <inheritdoc />
    public string Name => "Invariants — SlotFeedMissing";

    /// <inheritdoc />
    public string Description =>
        "Poules root + Quarts Places vides — SlotFeedMissing / PopulationShortfall / NonRootUnfed (Signals). Pas StructureGraphInvalid.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildInvariantsFeedMissingAsync(context, cancellationToken);
}
