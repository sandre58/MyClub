// -----------------------------------------------------------------------
// <copyright file="WorldCupTemplate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Templates;

/// <summary>
/// World Cup inspired Groups → KO + Bronze (Case 1: Top2 → R16 population → Slot Draw, no best thirds).
/// </summary>
/// <remarks>
/// Seed plays Groups→Qual population→Draw→R16→QF→SF→Final+Bronze with PlacementAwards (ranks 1–4) and Completes the
/// competition so Overview Terminée can show <c>CompetitionOutcome</c>. Product choice = Draw placement (not Auto
/// bracket). Auto Place / hybrid Case 7 = <c>qual-auto-place-mid</c> / <c>qual-hybrid-auto-draw-mid</c>.
/// Mid-bracket from-slots demo = <c>cup-qf-sf</c>. <c>:progress</c> is ignored — fixed seed.
/// </remarks>
public sealed class WorldCupTemplate : ICompetitionTemplate
{
    /// <inheritdoc />
    public string Id => "world-cup";

    /// <inheritdoc />
    public string Name => "FIFA World Cup";

    /// <inheritdoc />
    public string Description =>
        "Groups 8×4 → Top2 population → Slot Draw → R16→QF→SF → Final + Bronze · PlacementAwards 1–4 · Completed + Outcome (Case 1). :progress ignored.";

    /// <inheritdoc />
    public CompetitionRecipe Recipe { get; } = new()
    {
        DisplayName = "FIFA World Cup",
        Format = RecipeFormat.Groups,
        TeamCount = 32,
        GroupCount = 8,
        PlacesPerGroup = 4,
        StageName = "Phase de groupes",
        TeamNames = TeamNameSource.Dataset,
        DatasetCompetitionKey = "world-cup"
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildWorldCupAsync(context, cancellationToken);
}
