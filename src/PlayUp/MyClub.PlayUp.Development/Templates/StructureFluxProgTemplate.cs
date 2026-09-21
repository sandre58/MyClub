// -----------------------------------------------------------------------
// <copyright file="StructureFluxProgTemplate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Templates;

/// <summary>
/// Structure QA — Progression Sorties dialog (Demi → Finale / 3ᵉ place).
/// </summary>
public sealed class StructureFluxProgTemplate : ICompetitionTemplate
{
    /// <inheritdoc />
    public string Id => "structure-flux-prog";

    /// <inheritdoc />
    public string Name => "Structure — Progression Sorties";

    /// <inheritdoc />
    public string Description =>
        "Demi (fixtures) → Finale + Match 3ᵉ Places (Prog Auto) + Attribution · Draft. Ouvre le dialog Progression sur Demi-finales. :progress ignored.";

    /// <inheritdoc />
    public CompetitionRecipe Recipe { get; } = new()
    {
        DisplayName = "Structure — Progression Sorties",
        Format = RecipeFormat.Cup,
        TeamCount = 4,
        BracketSize = 4,
        StageName = "Demi-finales",
        TeamNames = TeamNameSource.Generated
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildFluxProgPlacementDraftAsync(context, cancellationToken);
}
