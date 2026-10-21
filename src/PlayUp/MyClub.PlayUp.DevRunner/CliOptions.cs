// -----------------------------------------------------------------------
// <copyright file="CliOptions.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using MyClub.PlayUp.Development.Runtime;

namespace MyClub.PlayUp.DevRunner;

/// <summary>
/// Parsed DevRunner command-line arguments.
/// </summary>
public sealed class CliOptions
{
    /// <summary>Gets a value indicating whether to reset the workspace database.</summary>
    public bool Reset { get; init; }

    /// <summary>Gets a value indicating whether to list scenarios.</summary>
    public bool List { get; init; }

    /// <summary>Gets a value indicating whether to list templates.</summary>
    public bool ListTemplates { get; init; }

    /// <summary>Gets a value indicating whether to show help.</summary>
    public bool ShowHelp { get; init; }

    /// <summary>Gets the optional determinism seed override.</summary>
    public int? Seed { get; init; }

    /// <summary>Gets scenario seed specs.</summary>
    public IReadOnlyList<SeedSpec> Scenarios { get; init; } = [];

    /// <summary>Gets template seed specs.</summary>
    public IReadOnlyList<SeedSpec> Templates { get; init; } = [];

    /// <summary>
    /// Parses CLI arguments.
    /// </summary>
    /// <param name="args">Raw args.</param>
    /// <returns>Parsed options.</returns>
    public static CliOptions Parse(string[] args)
    {
        var reset = false;
        var list = false;
        var listTemplates = false;
        var help = false;
        int? seed = null;
        var scenarios = new List<SeedSpec>();
        var templates = new List<SeedSpec>();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "-h" or "--help":
                    help = true;
                    continue;
                case "--reset":
                    reset = true;
                    continue;
                case "--list":
                    list = true;
                    continue;
                case "--list-templates":
                    listTemplates = true;
                    continue;
                case "--seed" when i + 1 >= args.Length:
                    throw new InvalidOperationException("--seed requires an integer value.");
                case "--seed":
                    seed = int.Parse(args[++i], CultureInfo.InvariantCulture);
                    continue;
                case "--scenarios" when i + 1 >= args.Length:
                    throw new InvalidOperationException("--scenarios requires a comma-separated id[:progress] list.");
                case "--scenarios":
                    scenarios.AddRange(ParseSpecs(args[++i]));
                    continue;
                case "--templates" when i + 1 >= args.Length:
                    throw new InvalidOperationException("--templates requires a comma-separated id[:progress] list.");
                case "--templates":
                    templates.AddRange(ParseSpecs(args[++i]));
                    continue;
                default:
                    throw new InvalidOperationException($"Unknown argument '{arg}'. Use --help.");
            }
        }

        return new CliOptions
        {
            Reset = reset,
            List = list,
            ListTemplates = listTemplates,
            ShowHelp = help,
            Seed = seed,
            Scenarios = scenarios,
            Templates = templates
        };
    }

    /// <summary>Writes CLI help to stdout.</summary>
    public static void PrintHelp() =>
#pragma warning disable CA1303
        Console.WriteLine(
            """
            MyClub.PlayUp.DevRunner — prepare the Development Workspace database.

              --reset                              Wipe + migrate (DB name must end with _dev)
              --templates <id[:progress][,…]>      Inspired competitions (datasets / capacity demos)
              --scenarios <id[:progress][,…]>      UX / métier / QA situations
              --seed <int>                         Determinism seed (default 42; clock seed if omitted with random)
              --list                               List scenarios
              --list-templates                     List templates
              --help                               This help

            Progress (progressive seeds): prepared | running | finished
            Default progress when omitted: running

            Templates = inspired competitions only (ligue-1, champions-league, world-cup, coupe-de-france, euro-across-groups).
            Scenarios = situations (lifecycle, Structure, Règlement, Confrontation, Flux).
            Scenario cup-qf-sf: QF played → SF slots filled (Overview materialize-from-slots demo).
            ConnectionStrings:PlayUpDev is required.
            """);
#pragma warning restore CA1303

    private static IEnumerable<SeedSpec> ParseSpecs(string raw) =>
        raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(SeedSpec.Parse);
}
