// -----------------------------------------------------------------------
// <copyright file="RandomScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Seed-driven format/size selection with selectable <see cref="SeedProgress"/>.
/// </summary>
public sealed class RandomScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "random";

    /// <inheritdoc />
    public string Name => "Random";

    /// <inheritdoc />
    public string Description => "Deterministic-from-seed format/size pick. Use :prepared|:running|:finished.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Random;

    /// <inheritdoc />
    public bool AcceptsProgress => true;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var recipe = BuildRecipe(context);
        return ScenarioOrchestration.BuildStructuredAsync(context, recipe, cancellationToken: cancellationToken);
    }

    private static CompetitionRecipe BuildRecipe(ScenarioContext context)
    {
        var pick = context.Entropy.Next(3);
        return pick switch
        {
            0 => new CompetitionRecipe
            {
                DisplayName = "Random championship",
                Format = RecipeFormat.Championship,
                TeamCount = 8,
                MatchdayCount = 7,
                StageName = "Championnat"
            },
            1 => new CompetitionRecipe
            {
                DisplayName = "Random groups",
                Format = RecipeFormat.Groups,
                TeamCount = 16,
                GroupCount = 4,
                PlacesPerGroup = 4,
                StageName = "Phase de groupes"
            },
            _ => new CompetitionRecipe
            {
                DisplayName = "Random cup",
                Format = RecipeFormat.Cup,
                TeamCount = 16,
                BracketSize = 16,
                StageName = "Tour à élimination"
            }
        };
    }
}
