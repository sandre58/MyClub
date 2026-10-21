// -----------------------------------------------------------------------
// <copyright file="CupDrawPendingScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Cup bracket structured without Slot draw — Draft, ready for draw.
/// </summary>
public sealed class CupDrawPendingScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "cup-draw-pending";

    /// <inheritdoc />
    public string Name => "Cup draw pending";

    /// <inheritdoc />
    public string Description =>
        "Cup 16 bracket + entries, composition empty (E0 Constituer), no Slot draw. Contrast: cup-composition-complete / cup:prepared.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildCupDrawPendingAsync(context, cancellationToken);
}
