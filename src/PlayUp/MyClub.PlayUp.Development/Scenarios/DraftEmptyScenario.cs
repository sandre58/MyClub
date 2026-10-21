// -----------------------------------------------------------------------
// <copyright file="DraftEmptyScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Orchestration;
using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Draft competition with no entries and no structure.
/// </summary>
public sealed class DraftEmptyScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "draft-empty";

    /// <inheritdoc />
    public string Name => "Draft empty";

    /// <inheritdoc />
    public string Description => "Newly created Draft competition without participants or structure.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Construction;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe { get; } = new()
    {
        DisplayName = "Draft vide",
        Format = RecipeFormat.Championship,
        TeamCount = 0
    };

    /// <inheritdoc />
    public async Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        await ScenarioOrchestration.CreateCompetitionAsync(
            context,
            Recipe!.DisplayName,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        await context.UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
