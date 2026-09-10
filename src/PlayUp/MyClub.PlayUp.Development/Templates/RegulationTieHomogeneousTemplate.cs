// -----------------------------------------------------------------------
// <copyright file="RegulationTieHomogeneousTemplate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Templates;

/// <summary>
/// QA seed for Règlement Confrontation tokens: one KO round with a rich TwoLegs TieFormat.
/// </summary>
public sealed class RegulationTieHomogeneousTemplate : ICompetitionTemplate
{
    /// <inheritdoc />
    public string Id => "regulation-tie-homogeneous";

    /// <inheritdoc />
    public string Name => "Démo Confrontation homogène";

    /// <inheritdoc />
    public string Description =>
        "Finale 1 round · A/R riche (agregat, buts ext., ET/TAB) · Draft — jetons Confrontation. :progress ignored.";

    /// <inheritdoc />
    public CompetitionRecipe Recipe { get; } = new()
    {
        DisplayName = "Démo Confrontation homogène",
        Format = RecipeFormat.Cup,
        TeamCount = 2,
        BracketSize = 2,
        StageName = "Finale",
        TeamNames = TeamNameSource.Generated
    };

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default) =>
        ScenarioOrchestration.BuildRegulationTieHomogeneousDemoAsync(context, cancellationToken);
}
