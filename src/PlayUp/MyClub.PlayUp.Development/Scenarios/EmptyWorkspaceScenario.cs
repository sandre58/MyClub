// -----------------------------------------------------------------------
// <copyright file="EmptyWorkspaceScenario.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Development.Recipes;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Scenarios;

/// <summary>
/// Leaves the workspace empty after reset.
/// </summary>
public sealed class EmptyWorkspaceScenario : IScenario
{
    /// <inheritdoc />
    public string Id => "empty-workspace";

    /// <inheritdoc />
    public string Name => "Empty workspace";

    /// <inheritdoc />
    public string Description => "No competitions — empty competition list.";

    /// <inheritdoc />
    public ScenarioCategory Category => ScenarioCategory.Workspace;

    /// <inheritdoc />
    public bool AcceptsProgress => false;

    /// <inheritdoc />
    public CompetitionRecipe? Recipe => null;

    /// <inheritdoc />
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        return context.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}
