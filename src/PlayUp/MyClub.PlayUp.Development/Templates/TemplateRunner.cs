// -----------------------------------------------------------------------
// <copyright file="TemplateRunner.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Development.Datasets;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.Development.Templates;

/// <summary>
/// Executes inspired competition templates atomically (fail-fast).
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TemplateRunner"/> class.
/// </remarks>
public sealed class TemplateRunner(
    IServiceScopeFactory scopeFactory,
    TemplateCatalog catalog,
    IWorkspaceStore workspaceStore,
    DatasetCatalog datasets,
    int workspaceSeed)
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly TemplateCatalog _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    private readonly IWorkspaceStore _workspaceStore = workspaceStore ?? throw new ArgumentNullException(nameof(workspaceStore));
    private readonly DatasetCatalog _datasets = datasets ?? throw new ArgumentNullException(nameof(datasets));

    /// <summary>
    /// Runs templates in order (no reset). Fail-fast; on failure clears the workspace.
    /// </summary>
    /// <param name="specs">Template seed specs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when templates are seeded.</returns>
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
    /// Resets then runs templates.
    /// </summary>
    /// <param name="specs">Template seed specs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when seeding finishes.</returns>
    public async Task ResetAndRunAsync(
        IEnumerable<SeedSpec> specs,
        CancellationToken cancellationToken = default)
    {
        await _workspaceStore.ResetAsync(cancellationToken).ConfigureAwait(false);
        await RunAsync(specs, cancellationToken).ConfigureAwait(false);
    }

    private Task ExecuteOneAsync(SeedSpec spec, CancellationToken cancellationToken)
    {
        var template = _catalog.Get(spec.Id);
        return SeedExecution.ExecuteAsync(
            _scopeFactory,
            _datasets,
            workspaceSeed,
            template.Id,
            spec.EffectiveProgress,
            template.ExecuteAsync,
            cancellationToken);
    }
}
