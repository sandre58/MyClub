// -----------------------------------------------------------------------
// <copyright file="TestCompetition.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.TestKit;

/// <summary>
/// In-memory competition situation for automated tests. Orchestrates Application use cases only.
/// </summary>
/// <remarks>
/// Lot B/C surface: create, teams, structure, competition Prepare/Start/Complete,
/// primary-stage Prepare/Start. Richer helpers emerge from later migrations —
/// do not invent a fluent DSL ahead of need.
/// </remarks>
public sealed class TestCompetition
{
    private static readonly DateTimeOffset DefaultEpoch =
        new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);

    private TestCompetition(Competition competition, IClock clock, Stage? primaryStage)
    {
        Competition = competition;
        Clock = clock;
        PrimaryStage = primaryStage;
    }

    /// <summary>Gets the competition aggregate.</summary>
    public Competition Competition { get; }

    /// <summary>Gets the primary structured stage when configured; otherwise null.</summary>
    public Stage? PrimaryStage { get; private set; }

    /// <summary>Gets the clock used for domain mutations.</summary>
    public IClock Clock { get; }

    /// <summary>
    /// Creates a Draft competition (bootstrap regulation unless overridden).
    /// </summary>
    /// <param name="name">Competition display name.</param>
    /// <param name="clock">Optional fixed clock; defaults to a stable epoch.</param>
    /// <param name="regulation">Optional regulation; defaults to <see cref="RegulationPacks.Standard"/>.</param>
    /// <returns>A new situation.</returns>
    public static TestCompetition Create(
        string name,
        IClock? clock = null,
        Regulation? regulation = null)
    {
        var resolvedClock = clock ?? new FixedClock(DefaultEpoch);
        var competition = regulation is null
            ? CreateCompetition.Execute(name, resolvedClock)
            : CreateCompetition.Execute(name, regulation, resolvedClock);
        return new TestCompetition(competition, resolvedClock, primaryStage: null);
    }

    /// <summary>
    /// Registers <paramref name="count"/> teams named <c>Team 1</c> … <c>Team N</c>.
    /// </summary>
    /// <param name="count">Number of teams.</param>
    /// <returns>This situation.</returns>
    public TestCompetition WithTeams(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        for (var i = 0; i < count; i++)
        {
            AddEntry.Execute(Competition, $"Team {i + 1}", Clock);
        }

        return this;
    }

    /// <summary>
    /// Registers the given team display names in order.
    /// </summary>
    /// <param name="names">Team display names.</param>
    /// <returns>This situation.</returns>
    public TestCompetition WithTeams(params string[] names)
    {
        ArgumentNullException.ThrowIfNull(names);
        foreach (var name in names)
        {
            AddEntry.Execute(Competition, name, Clock);
        }

        return this;
    }

    /// <summary>
    /// Applies <see cref="ConfigureStructure"/> and remembers the primary stage.
    /// </summary>
    /// <param name="intent">Structure intent.</param>
    /// <returns>This situation.</returns>
    public TestCompetition WithStructure(StructureIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        var result = ConfigureStructure.Execute(Competition, PrimaryStage, intent, Clock);
        PrimaryStage = result.Stage;
        return this;
    }

    /// <summary>
    /// Calls <see cref="Stage.Prepare"/> on the primary stage.
    /// </summary>
    /// <returns>This situation.</returns>
    public TestCompetition PreparePrimaryStage()
    {
        EnsurePrimaryStage().Prepare(Clock);
        return this;
    }

    /// <summary>
    /// Calls <see cref="Stage.Start"/> on the primary stage.
    /// </summary>
    /// <returns>This situation.</returns>
    public TestCompetition StartPrimaryStage()
    {
        EnsurePrimaryStage().Start(Clock);
        return this;
    }

    /// <summary>
    /// Calls <see cref="Competition.Prepare"/>.
    /// </summary>
    /// <returns>This situation.</returns>
    public TestCompetition PrepareCompetition()
    {
        Competition.Prepare(Clock);
        return this;
    }

    /// <summary>
    /// Calls <see cref="Competition.Start"/>.
    /// </summary>
    /// <returns>This situation.</returns>
    public TestCompetition StartCompetition()
    {
        Competition.Start(Clock);
        return this;
    }

    /// <summary>
    /// Calls <see cref="Competition.Complete"/>.
    /// </summary>
    /// <param name="mode">Completion mode.</param>
    /// <returns>This situation.</returns>
    public TestCompetition CompleteCompetition(CompletionMode mode)
    {
        Competition.Complete(mode, Clock);
        return this;
    }

    /// <summary>
    /// Returns the primary stage or throws if structure was not configured.
    /// </summary>
    /// <returns>The primary stage.</returns>
    public Stage RequirePrimaryStage() => EnsurePrimaryStage();

    private Stage EnsurePrimaryStage() =>
        PrimaryStage
        ?? throw new InvalidOperationException(
            "Primary stage is not configured. Call WithStructure(...) first.");
}
