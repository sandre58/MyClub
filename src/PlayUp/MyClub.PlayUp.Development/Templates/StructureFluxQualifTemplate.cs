// -----------------------------------------------------------------------
// <copyright file="StructureFluxQualifTemplate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Templates;

/// <summary>
/// Structure QA — Qualification Sorties dialog (Groups → QF Places).
/// </summary>
public sealed class StructureFluxQualifTemplate : ICompetitionTemplate
{
    /// <inheritdoc />
    public string Id => "structure-flux-qualif";

    /// <inheritdoc />
    public string Name => "Structure — Qualification Sorties";

    /// <inheritdoc />
    public string Description =>
        "Groups 2×4 Affectation + Qualif Top1/Top2 → QF Places · Draft. Ouvre le dialog Qualification sur la phase de groupes. :progress ignored.";

    /// <inheritdoc />
    public CompetitionRecipe Recipe { get; } = new()
    {
        DisplayName = "Structure — Qualification Sorties",
        Format = RecipeFormat.Groups,
        TeamCount = 8,
        GroupCount = 2,
        PlacesPerGroup = 4,
        StageName = "Phase de groupes",
        TeamNames = TeamNameSource.Generated
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildFluxQualifDraftAsync(context, cancellationToken);
}
