// -----------------------------------------------------------------------
// <copyright file="CoupeDeFranceTemplate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Templates;

/// <summary>
/// Coupe de France inspired multi-stage cup (R32 → R16 → QF → SF → Final).
/// </summary>
/// <remarks>
/// Seed stops after R32 is played and R16 slots are filled (Cockpit from-slots).
/// <c>:progress</c> is ignored — fixed seed like <c>cup-qf-sf</c>.
/// </remarks>
public sealed class CoupeDeFranceTemplate : ICompetitionTemplate
{
    /// <inheritdoc />
    public string Id => "coupe-de-france";

    /// <inheritdoc />
    public string Name => "Coupe de France";

    /// <inheritdoc />
    public string Description =>
        "Cup 32 (JSON) · R32→R16→QF→SF→Final · R32 played, R16 slots filled — stop before from-slots. :progress ignored.";

    /// <inheritdoc />
    public CompetitionRecipe Recipe { get; } = new()
    {
        DisplayName = "Coupe de France",
        Format = RecipeFormat.Cup,
        TeamCount = 32,
        BracketSize = 32,
        StageName = "32es de finale",
        TeamNames = TeamNameSource.Dataset,
        DatasetCompetitionKey = "coupe-de-france"
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildCoupeDeFranceMultiStageAsync(context, cancellationToken);
}
