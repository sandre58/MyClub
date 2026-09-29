// -----------------------------------------------------------------------
// <copyright file="TestCompetition.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.TestKit;

/// <summary>
/// In-memory competition situation for automated tests. Orchestrates Application use cases only.
/// </summary>
/// <remarks>
/// Lot B–F surface: create, teams, structure, multi-stage, Qual/Prog paths, resolved Slot Draw,
/// competition/stage lifecycle. Optional deterministic ids (DevSeed). Richer helpers emerge from
/// later migrations — do not invent a fluent DSL ahead of need.
/// </remarks>
public sealed class TestCompetition
{
    private static readonly DateTimeOffset DefaultEpoch =
        new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);

    private readonly List<Stage> _stages = [];
    private readonly List<Match> _matches = [];

    private TestCompetition(Competition competition, IClock clock, Stage? primaryStage)
    {
        Competition = competition;
        Clock = clock;
        PrimaryStage = primaryStage;
        if (primaryStage is not null)
        {
            _stages.Add(primaryStage);
        }
    }

    /// <summary>Gets the competition aggregate.</summary>
    public Competition Competition { get; }

    /// <summary>Gets the primary structured stage when configured; otherwise null.</summary>
    public Stage? PrimaryStage { get; private set; }

    /// <summary>Gets tracked stages (primary and additional) for consumer persist.</summary>
    public IReadOnlyList<Stage> Stages => _stages;

    /// <summary>Gets tracked matches for consumer persist.</summary>
    public IReadOnlyList<Match> Matches => _matches;

    /// <summary>Gets the clock used for domain mutations.</summary>
    public IClock Clock { get; }

    /// <summary>
    /// Creates a Draft competition (bootstrap regulation unless overridden).
    /// </summary>
    /// <param name="name">Competition display name.</param>
    /// <param name="clock">Optional fixed clock; defaults to a stable epoch.</param>
    /// <param name="regulation">Optional regulation; defaults to <see cref="RegulationPacks.Standard"/>.</param>
    /// <param name="competitionId">Optional explicit competition identity (deterministic seeds).</param>
    /// <returns>A new situation.</returns>
    public static TestCompetition Create(
        string name,
        IClock? clock = null,
        Regulation? regulation = null,
        CompetitionId? competitionId = null)
    {
        var resolvedClock = clock ?? new FixedClock(DefaultEpoch);
        var resolvedRegulation = regulation ?? RegulationPacks.Standard();
        var competition = CreateCompetition.Execute(name, resolvedRegulation, resolvedClock, competitionId);
        return new TestCompetition(competition, resolvedClock, primaryStage: null);
    }

    /// <summary>
    /// Wraps an existing competition aggregate (DevSeed enrichment after TestKit birth).
    /// </summary>
    /// <param name="competition">Competition already created.</param>
    /// <param name="clock">Clock for further mutations.</param>
    /// <param name="primaryStage">Optional primary stage already attached.</param>
    /// <returns>A situation bound to the given aggregates.</returns>
    public static TestCompetition For(
        Competition competition,
        IClock clock,
        Stage? primaryStage = null)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(clock);
        return new TestCompetition(competition, clock, primaryStage);
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
    /// Registers one team with optional deterministic ids and presentation (DevSeed).
    /// </summary>
    /// <param name="displayName">Entry display name.</param>
    /// <param name="teamId">Optional team identity.</param>
    /// <param name="entryId">Optional entry identity.</param>
    /// <param name="presentation">Optional presentation metadata.</param>
    /// <returns>This situation.</returns>
    public TestCompetition WithTeam(
        string displayName,
        TeamId? teamId = null,
        EntryId? entryId = null,
        EntryPresentation? presentation = null)
    {
        AddEntry.Execute(Competition, displayName, Clock, teamId, presentation, entryId);
        return this;
    }

    /// <summary>
    /// Applies <see cref="ConfigureStructure"/> and remembers the primary stage.
    /// </summary>
    /// <param name="intent">Structure intent.</param>
    /// <param name="stageId">
    /// Optional explicit stage identity. When set, pre-creates the primary stage before ConfigureStructure
    /// (DevSeed deterministic ids).
    /// </param>
    /// <returns>This situation.</returns>
    public TestCompetition WithStructure(StructureIntent intent, StageId? stageId = null)
    {
        ArgumentNullException.ThrowIfNull(intent);

        var seed = PrimaryStage;
        if (seed is null && stageId is { } explicitStageId)
        {
            // Match DevSeed ConfigurePrimaryStage: Regulation overload (classifying), then ConfigureStructure.
            seed = Stage.Create(
                Competition.Id,
                new StageName(intent.StageName),
                Competition.Regulation,
                explicitStageId,
                Clock);
            Competition.AddStage(seed.Id, Clock);
        }

        var result = ConfigureStructure.Execute(Competition, seed, intent, Clock);
        PrimaryStage = result.Stage;
        TrackStage(result.Stage);
        return this;
    }

    /// <summary>
    /// Adds a bare Draft stage (no structure skeleton) and tracks it.
    /// </summary>
    /// <param name="name">Stage display name.</param>
    /// <param name="stageId">Optional explicit stage identity.</param>
    /// <returns>The created stage.</returns>
    public Stage AddBareStage(string name, StageId? stageId = null)
    {
        var stage = stageId is { } id
            ? Stage.Create(Competition.Id, new StageName(name), Competition.Regulation, id, Clock)
            : Stage.Create(Competition.Id, new StageName(name), Competition.Regulation, Clock);
        Competition.AddStage(stage.Id, Clock);
        TrackStage(stage);
        PrimaryStage ??= stage;
        return stage;
    }

    /// <summary>
    /// Adds a knockout stage (round + slots + optional bracket pairs) and tracks it.
    /// </summary>
    /// <param name="name">Stage display name.</param>
    /// <param name="roundName">Entry round name.</param>
    /// <param name="slotKeys">Slot keys in order.</param>
    /// <param name="stageId">Optional explicit stage identity.</param>
    /// <param name="seedBracketPairs">When true, calls <see cref="Stage.SeedEntryRoundBracketPairs"/>.</param>
    /// <returns>The created stage.</returns>
    public Stage AddKnockoutStage(
        string name,
        string roundName,
        IReadOnlyList<string> slotKeys,
        StageId? stageId = null,
        bool seedBracketPairs = true)
    {
        ArgumentNullException.ThrowIfNull(slotKeys);
        var stage = stageId is { } id
            ? Stage.Create(Competition.Id, new StageName(name), Competition.Regulation, id, Clock)
            : Stage.Create(Competition.Id, new StageName(name), Competition.Regulation, Clock);
        stage.AddRound(roundName, new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), Clock);
        foreach (var key in slotKeys)
        {
            stage.AddSlot(key);
        }

        if (seedBracketPairs)
        {
            stage.SeedEntryRoundBracketPairs();
        }

        Competition.AddStage(stage.Id, Clock);
        TrackStage(stage);
        PrimaryStage ??= stage;
        return stage;
    }

    /// <summary>
    /// Replaces qualification paths on <paramref name="stage"/> via Application authoring.
    /// </summary>
    /// <param name="stage">Source stage.</param>
    /// <param name="paths">Qualification path specs.</param>
    /// <returns>This situation.</returns>
    public TestCompetition WithQualificationPaths(Stage stage, params QualificationPathSpec[] paths)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(paths);
        ReplaceStageQualificationRules.Execute(stage, paths, Clock);
        return this;
    }

    /// <summary>
    /// Replaces progression paths on <paramref name="stage"/> via Application authoring.
    /// </summary>
    /// <param name="stage">Source stage.</param>
    /// <param name="paths">Progression path specs.</param>
    /// <returns>This situation.</returns>
    public TestCompetition WithProgressionPaths(Stage stage, params ProgressionPathSpec[] paths)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(paths);
        ReplaceStageProgressionRules.Execute(stage, paths, Clock);
        return this;
    }

    /// <summary>
    /// Creates a Slot Draw in Draft with a deterministic Resolved resolution (no Publish/Apply).
    /// </summary>
    /// <param name="stage">Stage receiving the draw.</param>
    /// <param name="pool">Entry pool (ordered to match <paramref name="slotKeys"/>).</param>
    /// <param name="slotKeys">Target slot keys.</param>
    /// <param name="drawId">Optional explicit draw identity.</param>
    /// <returns>The created draw identity.</returns>
    public DrawId CreateResolvedSlotDraw(
        Stage stage,
        IReadOnlyList<EntryId> pool,
        IReadOnlyList<string> slotKeys,
        DrawId? drawId = null)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(pool);
        ArgumentNullException.ThrowIfNull(slotKeys);
        if (pool.Count != slotKeys.Count)
        {
            throw new InvalidOperationException(
                $"Slot draw pool count ({pool.Count}) must match slot key count ({slotKeys.Count}).");
        }

        var draw = drawId is { } id
            ? stage.CreateDraw(DrawResolutionKind.Slot, id, Clock)
            : stage.CreateDraw(DrawResolutionKind.Slot, Clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([.. pool]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots(
                [.. pool.Select((entryId, index) => new SlotDrawPlacement(entryId, slotKeys[index]))]),
            Clock);
        return draw.Id;
    }

    /// <summary>
    /// Tracks a match for consumer persistence (Host.Tests / DevSeed).
    /// </summary>
    /// <param name="match">Match to track.</param>
    /// <returns>This situation.</returns>
    public TestCompetition TrackMatch(Match match)
    {
        ArgumentNullException.ThrowIfNull(match);
        if (!_matches.Exists(m => m.Id.Equals(match.Id)))
        {
            _matches.Add(match);
        }

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

    private void TrackStage(Stage stage)
    {
        if (!_stages.Exists(s => s.Id.Equals(stage.Id)))
        {
            _stages.Add(stage);
        }
    }

    private Stage EnsurePrimaryStage() =>
        PrimaryStage
        ?? throw new InvalidOperationException(
            "Primary stage is not configured. Call WithStructure(...) first.");
}
