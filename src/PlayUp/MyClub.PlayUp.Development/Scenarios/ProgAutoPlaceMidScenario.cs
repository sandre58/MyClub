// -----------------------------------------------------------------------
// <copyright file="ProgAutoPlaceMidScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Case 3: QF winners Prog Auto Place into SF slots (dual-write), no orchestration Place helper.
/// </summary>
public sealed class ProgAutoPlaceMidScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "prog-auto-place-mid";

    /// <inheritdoc />
    public string Name => "Prog Auto Place mid";

    /// <inheritdoc />
    public string Description =>
        "8 teams · QF played · winners Prog Auto Place → SF slots (dual-write) — SF Draft. WhoFeeds = Prog.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Operational;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildProgAutoPlaceMidAsync(context, cancellationToken);
}
