// -----------------------------------------------------------------------
// <copyright file="ScenarioOrchestration.GroupsManualPlacement.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Development.Orchestration;

internal static partial class ScenarioOrchestration
{
    /// <summary>
    /// Three group competitions for the manual placement screen:
    /// empty pools, pot rules with an empty grid, and a Ready pool that already has a match.
    /// </summary>
    public static async Task BuildGroupsManualPlacementAsync(
        ScenarioContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        await SeedAsync(
            context,
            "manual",
            "Placement groupes — manuel",
            withPotRules: false,
            withMatch: false,
            cancellationToken).ConfigureAwait(false);
        await SeedAsync(
            context,
            "draw",
            "Placement groupes — tirage",
            withPotRules: true,
            withMatch: false,
            cancellationToken).ConfigureAwait(false);
        await SeedAsync(
            context,
            "match",
            "Placement groupes — match",
            withPotRules: false,
            withMatch: true,
            cancellationToken).ConfigureAwait(false);

        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task SeedAsync(
        ScenarioContext context,
        string scope,
        string displayName,
        bool withPotRules,
        bool withMatch,
        CancellationToken cancellationToken)
    {
        var recipe = new CompetitionRecipe
        {
            DisplayName = displayName,
            Format = RecipeFormat.Groups,
            TeamCount = 8,
            GroupCount = 2,
            PlacesPerGroup = 4,
            StageName = "Phase de groupes",
            TeamNames = TeamNameSource.Generated,
        };

        var competition = await CreateCompetitionFromRecipeAsync(
            context,
            recipe,
            cancellationToken,
            competitionLocalKey: scope).ConfigureAwait(false);
        var entries = await RegisterTeamsAsync(
            context,
            competition,
            recipe,
            identityScope: scope,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        var stage = ConfigurePrimaryStage(context, competition, recipe, stageLocalKey: scope);
        AssignRootComposition(stage, entries, context.Clock);

        if (withPotRules)
        {
            stage.ReplaceDrawRules(
                new DrawRules(DrawMode.Random, potRules: new PotRules(4)),
                context.Clock);
        }

        if (!withMatch)
        {
            return;
        }

        var groupA = stage.Groups[0];
        for (var index = 0; index < 3; index++)
        {
            stage.AssignEntryToGroup(groupA.Id, entries[index].Id);
        }

        SeedMaterialization.MaterializeGroupsMatches(
            context,
            competition,
            stage,
            matchIdentityScope: scope);
        PrepareStage.Execute(stage, [stage], context.Clock);
        PrepareCompetition.Execute(competition, context.Clock);
    }
}
