// -----------------------------------------------------------------------
// <copyright file="ScenarioRunner.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Development.Datasets;

namespace MyClub.PlayUp.Development.Runtime;

/// <summary>
/// Executes Development Workspace scenarios atomically (fail-fast).
/// </summary>
public sealed class ScenarioRunner
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ScenarioCatalog _catalog;
    private readonly IWorkspaceStore _workspaceStore;
    private readonly DatasetCatalog _datasets;
    private readonly int _workspaceSeed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScenarioRunner"/> class.
    /// </summary>
    public ScenarioRunner(
        IServiceScopeFactory scopeFactory,
        ScenarioCatalog catalog,
        IWorkspaceStore workspaceStore,
        DatasetCatalog datasets,
        int workspaceSeed)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _workspaceStore = workspaceStore ?? throw new ArgumentNullException(nameof(workspaceStore));
        _datasets = datasets ?? throw new ArgumentNullException(nameof(datasets));
        _workspaceSeed = workspaceSeed;
    }

    /// <summary>
    /// Runs scenarios in order (no reset). Fail-fast; on failure clears the workspace.
    /// </summary>
    /// <param name="specs">Scenario seed specs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when all scenarios are seeded.</returns>
    public async Task RunAsync(IEnumerable<SeedSpec> specs, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specs);
        var items = specs.ToArray();
        try
        {
            foreach (var spec in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ExecuteOneAsync(spec, cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            await _workspaceStore.ResetAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Resets the workspace then runs the given scenarios.
    /// </summary>
    /// <param name="specs">Scenario seed specs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when seeding finishes.</returns>
    public async Task ResetAndRunAsync(
        IEnumerable<SeedSpec> specs,
        CancellationToken cancellationToken = default)
    {
        await _workspaceStore.ResetAsync(cancellationToken).ConfigureAwait(false);
        await RunAsync(specs, cancellationToken).ConfigureAwait(false);
    }

    private async Task ExecuteOneAsync(SeedSpec spec, CancellationToken cancellationToken)
    {
        var scenario = _catalog.Get(spec.Id);
        if (!scenario.AcceptsProgress && spec.Progress is not null)
        {
            throw new InvalidOperationException(
                $"Scenario '{scenario.Id}' does not accept progress (got '{spec.Progress}').");
        }

        var progress = scenario.AcceptsProgress ? spec.EffectiveProgress : SeedProgress.Prepared;
        await SeedExecution.ExecuteAsync(
            _scopeFactory,
            _datasets,
            _workspaceSeed,
            scenario.Id,
            progress,
            scenario.ExecuteAsync,
            cancellationToken).ConfigureAwait(false);
    }
}
