// -----------------------------------------------------------------------
// <copyright file="CompetitionRecipeValidator.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;

namespace MyClub.PlayUp.Development.Recipes;

/// <summary>
/// Validates recipes and maps them to Application <see cref="StructureIntent"/>.
/// </summary>
public static class CompetitionRecipeValidator
{
    /// <summary>
    /// Validates the recipe and returns a <see cref="StructureIntent"/>.
    /// </summary>
    /// <param name="recipe">Recipe to validate.</param>
    /// <returns>Validated structure intent.</returns>
    public static StructureIntent ToStructureIntent(CompetitionRecipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentException.ThrowIfNullOrWhiteSpace(recipe.DisplayName);

        return recipe.TeamCount < 0
            ? throw new InvalidOperationException("Recipe TeamCount cannot be negative.")
            : recipe.Format switch
        {
            RecipeFormat.Championship => ValidateChampionship(recipe),
            RecipeFormat.Groups => ValidateGroups(recipe),
            RecipeFormat.Cup => ValidateCup(recipe),
            RecipeFormat.Swiss => ValidateSwiss(recipe),
            _ => throw new InvalidOperationException($"Unsupported recipe format '{recipe.Format}'.")
        };
    }

    private static StructureIntent ValidateChampionship(CompetitionRecipe recipe)
    {
        var matchdays = recipe.MatchdayCount ?? 1;
        return matchdays < 1
            ? throw new InvalidOperationException("Championship requires MatchdayCount >= 1.")
            : StructureIntent.Championship(matchdays, recipe.StageName, recipe.MatchGenerationFormat);
    }

    private static StructureIntent ValidateGroups(CompetitionRecipe recipe)
    {
        var groups = recipe.GroupCount
            ?? throw new InvalidOperationException("Groups recipe requires GroupCount.");
        var perGroup = recipe.PlacesPerGroup
            ?? throw new InvalidOperationException("Groups recipe requires PlacesPerGroup.");

        return groups * perGroup != recipe.TeamCount
            ? throw new InvalidOperationException(
                $"Groups recipe requires GroupCount × PlacesPerGroup == TeamCount ({groups}×{perGroup}≠{recipe.TeamCount}).")
            : StructureIntent.Groups(groups, perGroup, recipe.StageName, recipe.MatchGenerationFormat);
    }

    private static StructureIntent ValidateCup(CompetitionRecipe recipe)
    {
        var bracket = recipe.BracketSize ?? recipe.TeamCount;
        return bracket != recipe.TeamCount
            ? throw new InvalidOperationException(
                $"Cup recipe requires TeamCount == BracketSize ({recipe.TeamCount}≠{bracket}).")
            : StructureIntent.Cup(bracket, recipe.StageName);
    }

    private static StructureIntent ValidateSwiss(CompetitionRecipe recipe)
    {
        if (recipe.TeamCount < 2)
        {
            throw new InvalidOperationException("Swiss recipe requires TeamCount >= 2.");
        }

        var rounds = recipe.SwissRoundCount
            ?? throw new InvalidOperationException("Swiss recipe requires SwissRoundCount.");
        return rounds < 1
            ? throw new InvalidOperationException("Swiss recipe requires SwissRoundCount >= 1.")
            : StructureIntent.Swiss(rounds, recipe.StageName);
    }
}
