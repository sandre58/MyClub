// -----------------------------------------------------------------------
// <copyright file="CompetitionRecipeValidatorTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Development.Recipes;
using Xunit;

namespace MyClub.PlayUp.Development.Tests;

public sealed class CompetitionRecipeValidatorTests
{
    [Fact]
    public void Swiss_maps_to_StructureIntent_Swiss()
    {
        var intent = CompetitionRecipeValidator.ToStructureIntent(
            new CompetitionRecipe
            {
                DisplayName = "Swiss 8×3",
                Format = RecipeFormat.Swiss,
                TeamCount = 8,
                SwissRoundCount = 3,
                StageName = "Swiss"
            });

        intent.Format.Should().Be(StructureFormatKind.Swiss);
        intent.SwissRoundCount.Should().Be(3);
        intent.StageName.Should().Be("Swiss");
    }

    [Fact]
    public void Swiss_requires_round_count()
    {
        var act = () => CompetitionRecipeValidator.ToStructureIntent(
            new CompetitionRecipe
            {
                DisplayName = "Swiss",
                Format = RecipeFormat.Swiss,
                TeamCount = 8
            });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SwissRoundCount*");
    }
}
