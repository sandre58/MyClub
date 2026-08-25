// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Development.Templates;
using MyClub.PlayUp.DevRunner;
using MyClub.PlayUp.Infrastructure.DependencyInjection;

// Usage:
//   --reset --templates ligue-1:prepared --scenarios groups:running
//   --scenarios random:finished --seed 7
//   --list / --list-templates
var options = CliOptions.Parse(args);
if (options.ShowHelp)
{
    CliOptions.PrintHelp();
    return;
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets(typeof(Program).Assembly, optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = configuration.GetConnectionString(DevDatabaseGuard.ConnectionStringName);
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        $"Connection string '{DevDatabaseGuard.ConnectionStringName}' is not configured. "
        + "Set User Secrets / env ConnectionStrings__PlayUpDev "
        + $"(database name must end with '{DevDatabaseGuard.RequiredDatabaseNameSuffix}').");
}

DevDatabaseGuard.ValidateForDestructiveUse(connectionString, environmentName: "Development");

var wantsRandom = options.Scenarios.Any(static s =>
    string.Equals(s.Id, "random", StringComparison.OrdinalIgnoreCase));
var seed = options.Seed
    ?? (wantsRandom
        ? Environment.TickCount
        : configuration.GetValue("PlayUp:Development:Seed", 42));
if (options.Seed is null && wantsRandom)
{
#pragma warning disable CA1303
    Console.WriteLine($"Using clock-derived seed={seed} for random scenario.");
#pragma warning restore CA1303
}

var workspace = new DevelopmentWorkspaceOptions { Seed = seed };

var services = new ServiceCollection();
services.AddPlayUpInfrastructure(connectionString);
services.AddSingleton<IWorkspaceStore, PostgresWorkspaceStore>();
services.AddPlayUpDevelopmentWorkspace(workspace);
await using var provider = services.BuildServiceProvider();

var scenarioCatalog = provider.GetRequiredService<ScenarioCatalog>();
var templateCatalog = provider.GetRequiredService<TemplateCatalog>();

if (options.List)
{
    foreach (var scenario in scenarioCatalog.All.OrderBy(static s => s.Id, StringComparer.Ordinal))
    {
        var progressNote = scenario.AcceptsProgress ? "progress=prepared|running|finished" : "no-progress";
        Console.WriteLine($"{scenario.Id}\t{scenario.Category}\t{progressNote}\t{scenario.Description}");
    }

    return;
}

if (options.ListTemplates)
{
    foreach (var template in templateCatalog.All.OrderBy(static t => t.Id, StringComparer.Ordinal))
    {
        Console.WriteLine($"{template.Id}\tprogress=prepared|running|finished\t{template.Description}");
    }

    return;
}

var hasWork = options.Templates.Count > 0 || options.Scenarios.Count > 0;
if (options.Reset && !hasWork)
{
    await provider.GetRequiredService<IWorkspaceStore>().ResetAsync().ConfigureAwait(false);
#pragma warning disable CA1303
    Console.WriteLine("Workspace reset (no templates/scenarios).");
#pragma warning restore CA1303
    return;
}

if (!hasWork)
{
    CliOptions.PrintHelp();
    throw new InvalidOperationException("Specify --templates, --scenarios, --reset, --list, and/or --list-templates.");
}

if (options.Reset)
{
    await provider.GetRequiredService<IWorkspaceStore>().ResetAsync().ConfigureAwait(false);
}

var templateRunner = provider.GetRequiredService<TemplateRunner>();
var scenarioRunner = provider.GetRequiredService<ScenarioRunner>();

if (options.Templates.Count > 0)
{
    await templateRunner.RunAsync(options.Templates).ConfigureAwait(false);
}

if (options.Scenarios.Count > 0)
{
    await scenarioRunner.RunAsync(options.Scenarios).ConfigureAwait(false);
}

using var scope = provider.CreateScope();
var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
foreach (var competition in await competitions.ListAsync().ConfigureAwait(false))
{
    Console.WriteLine($"competitionId={competition.Id.Value}");
}
