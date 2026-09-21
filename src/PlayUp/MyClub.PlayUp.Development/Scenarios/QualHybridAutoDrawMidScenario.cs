// -----------------------------------------------------------------------
// <copyright file="QualHybridAutoDrawMidScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Case 7: partial Qual Auto Place + Slot Draw of remaining Population into empty Places.
/// </summary>
public sealed class QualHybridAutoDrawMidScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "qual-hybrid-auto-draw-mid";

    /// <inheritdoc />
    public string Name => "Qual hybride Auto + Tirage mid";

    /// <inheritdoc />
    public string Description =>
        "Groups 2×4 finished → Top1 Auto Place + Top2 Population → Slot Draw fills remaining QF Places — KO Draft. WhoFeeds = Qual | Draw.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Operational;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildQualHybridAutoDrawMidAsync(context, cancellationToken);
}
