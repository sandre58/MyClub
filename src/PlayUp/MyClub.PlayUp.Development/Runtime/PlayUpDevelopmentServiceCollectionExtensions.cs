// -----------------------------------------------------------------------
// <copyright file="PlayUpDevelopmentServiceCollectionExtensions.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Development.Datasets;
using MyClub.PlayUp.Development.Scenarios;
using MyClub.PlayUp.Development.Templates;

namespace MyClub.PlayUp.Development.Runtime;

/// <summary>
/// DI registration for Development Workspace catalogs and runners.
/// </summary>
public static class PlayUpDevelopmentServiceCollectionExtensions
{
    /// <summary>
    /// Registers datasets, scenarios, templates, catalogs, and runners.
    /// Requires <see cref="IWorkspaceStore"/> and Application ports already registered.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="options">Workspace options.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddPlayUpDevelopmentWorkspace(
        this IServiceCollection services,
        DevelopmentWorkspaceOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddSingleton(options);
        services.AddSingleton(_ => DatasetCatalog.LoadFromAssembly(typeof(DatasetCatalog).Assembly));

        services.AddSingleton<IScenario, EmptyWorkspaceScenario>();
        services.AddSingleton<IScenario, DraftEmptyScenario>();
        services.AddSingleton<IScenario, RegistrationOpenScenario>();
        services.AddSingleton<IScenario, ChampionshipScenario>();
        services.AddSingleton<IScenario, GroupsScenario>();
        services.AddSingleton<IScenario, CupScenario>();
        services.AddSingleton<IScenario, RandomScenario>();
        services.AddSingleton(static sp => new ScenarioCatalog(sp.GetServices<IScenario>()));

        services.AddSingleton<ICompetitionTemplate, Ligue1Template>();
        services.AddSingleton<ICompetitionTemplate, ChampionsLeagueTemplate>();
        services.AddSingleton<ICompetitionTemplate, WorldCupTemplate>();
        services.AddSingleton<ICompetitionTemplate, CoupeDeFranceTemplate>();
        services.AddSingleton(static sp => new TemplateCatalog(sp.GetServices<ICompetitionTemplate>()));

        services.AddSingleton(sp => new ScenarioRunner(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<ScenarioCatalog>(),
            sp.GetRequiredService<IWorkspaceStore>(),
            sp.GetRequiredService<DatasetCatalog>(),
            options.Seed));
        services.AddSingleton(sp => new TemplateRunner(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<TemplateCatalog>(),
            sp.GetRequiredService<IWorkspaceStore>(),
            sp.GetRequiredService<DatasetCatalog>(),
            options.Seed));

        return services;
    }
}
