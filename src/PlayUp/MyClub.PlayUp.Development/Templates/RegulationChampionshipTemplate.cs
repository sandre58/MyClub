// -----------------------------------------------------------------------
// <copyright file="RegulationChampionshipTemplate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Development.Templates;

/// <summary>
/// Lightweight championship Draft seed for Règlement hub schematic QA.
/// </summary>
public sealed class RegulationChampionshipTemplate : ICompetitionTemplate
{
    /// <inheritdoc />
    public string Id => "regulation-championship";

    /// <inheritdoc />
    public string Name => "Démo Championnat";

    /// <inheritdoc />
    public string Description =>
        "Championnat 8 équipes · Double RR · Draft — schéma hub Règlement. :progress ignored.";

    /// <inheritdoc />
    public CompetitionRecipe Recipe { get; } = new()
    {
        DisplayName = "Démo Championnat",
        Format = RecipeFormat.Championship,
        TeamCount = 8,
        MatchdayCount = 14,
        MatchGenerationFormat = MatchGenerationFormat.DoubleRoundRobin,
        StageName = "Championnat",
        TeamNames = TeamNameSource.Generated
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildRegulationChampionshipDemoAsync(context, cancellationToken);
}
