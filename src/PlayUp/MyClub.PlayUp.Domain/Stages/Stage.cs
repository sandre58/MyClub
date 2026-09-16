// -----------------------------------------------------------------------
// <copyright file="Stage.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages.Events;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Aggregate root for a competition phase: lifecycle, structure, and materialized regulation.
/// </summary>
[DebuggerDisplay("{Name} ({Status})")]
public sealed class Stage : AggregateRoot<StageId>
{
    private readonly List<Group> _groups = [];
    private readonly List<Round> _rounds = [];
    private readonly List<Matchday> _matchdays = [];
    private readonly List<Slot> _slots = [];
    private readonly List<DirectAssignment> _directAssignments = [];
    private readonly List<CompositionEntry> _compositionEntries = [];
    private readonly List<Draw> _draws = [];
    private readonly List<Penalty> _penalties = [];
    private readonly List<MatchPlacement> _matchPlacements = [];
    private readonly List<SwissBye> _swissByeHistory = [];

    private Stage(
        StageId id,
        CompetitionId competitionId,
        StageName name,
        StageRegulation regulation,
        DefaultsBinding defaultsBinding)
        : base(id)
    {
        CompetitionId = competitionId;
        Name = name;
        Regulation = regulation;
        DefaultsBinding = defaultsBinding;
        Status = StageStatus.Draft;
        MatchGenerationFormat = MatchGenerationFormat.SingleRoundRobin;
    }

    /// <summary>
    /// Gets the owning competition identity (immutable).
    /// </summary>
    public CompetitionId CompetitionId { get; }

    /// <summary>
    /// Gets the stage name.
    /// </summary>
    public StageName Name { get; private set; }

    /// <summary>
    /// Gets the materialized stage regulation (independent from the competition regulation).
    /// </summary>
    public StageRegulation Regulation { get; private set; }

    /// <summary>
    /// Gets which heritable regulation parts still follow Competition defaults.
    /// </summary>
    public DefaultsBinding DefaultsBinding { get; private set; }

    /// <summary>
    /// Gets the stage lifecycle status.
    /// </summary>
    public StageStatus Status { get; private set; }

    /// <summary>
    /// Gets how Championship / Groups matches are generated for this stage.
    /// </summary>
    /// <remarks>Cup and Swiss materialization ignore this value. Default is <see cref="MatchGenerationFormat.SingleRoundRobin"/>.</remarks>
    public MatchGenerationFormat MatchGenerationFormat { get; private set; }

    /// <summary>
    /// Gets Swiss Kind settings when this stage is Swiss; otherwise <see langword="null"/>.
    /// </summary>
    public SwissSettings? SwissSettings { get; private set; }

    /// <summary>
    /// Gets structural places per group for Groups form capacity (Places N = groupCount × this).
    /// Independent of DrawRules — Clear Draw must not clear this.
    /// </summary>
    public int? PlacesPerGroup { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this stage is configured as Swiss Kind.
    /// </summary>
    public bool IsSwiss => SwissSettings is not null;

    /// <summary>
    /// Gets the groups in this stage.
    /// </summary>
    public IReadOnlyList<Group> Groups => _groups.AsReadOnly();

    /// <summary>
    /// Gets the rounds in this stage.
    /// </summary>
    public IReadOnlyList<Round> Rounds => _rounds.AsReadOnly();

    /// <summary>
    /// Gets the matchdays in this stage.
    /// </summary>
    public IReadOnlyList<Matchday> Matchdays => _matchdays.AsReadOnly();

    /// <summary>
    /// Gets the positional slots in this stage.
    /// </summary>
    public IReadOnlyList<Slot> Slots => _slots.AsReadOnly();

    /// <summary>
    /// Gets the direct slot assignments (configuration feeds).
    /// </summary>
    public IReadOnlyList<DirectAssignment> DirectAssignments => _directAssignments.AsReadOnly();

    /// <summary>
    /// Gets the root composition entry set (who constitutes the phase before Draw).
    /// </summary>
    public IReadOnlyList<CompositionEntry> CompositionEntries => _compositionEntries.AsReadOnly();

    /// <summary>
    /// Gets the draws owned by this stage.
    /// </summary>
    public IReadOnlyList<Draw> Draws => _draws.AsReadOnly();

    /// <summary>
    /// Gets the standing penalties owned by this stage.
    /// Applicable solely by presence in this collection (no Active/Revoked status).
    /// </summary>
    public IReadOnlyList<Penalty> Penalties => _penalties.AsReadOnly();

    /// <summary>
    /// Gets materialized calendar placements (one per attached <see cref="MatchId"/>).
    /// </summary>
    public IReadOnlyList<MatchPlacement> MatchPlacements => _matchPlacements.AsReadOnly();

    /// <summary>
    /// Gets recorded Swiss byes (pairing events — no Fixture/Match).
    /// </summary>
    public IReadOnlyList<SwissBye> SwissByeHistory => _swissByeHistory.AsReadOnly();

    private bool HasStructure => _groups.Count > 0 || _rounds.Count > 0 || _matchdays.Count > 0;

    /// <summary>
    /// Creates a new stage in Draft status with a regulation materialized from the competition.
    /// </summary>
    /// <param name="competitionId">The owning competition identity.</param>
    /// <param name="name">The stage name.</param>
    /// <param name="competitionRegulation">The competition regulation to materialize from.</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <returns>The created stage.</returns>
    public static Stage Create(
        CompetitionId competitionId,
        StageName name,
        Regulation competitionRegulation,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competitionRegulation);
        const bool classifying = true;
        return Create(
            competitionId,
            name,
            StageRegulation.MaterializeFrom(competitionRegulation),
            DefaultsBinding.AllBound(classifying),
            clock);
    }

    /// <summary>
    /// Creates a new stage in Draft status with a generated identity, materializing regulation from the competition.
    /// </summary>
    /// <param name="competitionId">The owning competition identity.</param>
    /// <param name="name">The stage name.</param>
    /// <param name="competitionRegulation">The competition regulation to materialize from.</param>
    /// <param name="id">The stage identity (must not be empty).</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <returns>The created stage.</returns>
    public static Stage Create(
        CompetitionId competitionId,
        StageName name,
        Regulation competitionRegulation,
        StageId id,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competitionRegulation);
        const bool classifying = true;
        return Create(
            competitionId,
            name,
            StageRegulation.MaterializeFrom(competitionRegulation),
            DefaultsBinding.AllBound(classifying),
            id,
            clock);
    }

    /// <summary>
    /// Creates a new stage in Draft status with an independent copy of the given stage regulation.
    /// Binding defaults to all-bound for classifying capacity when standing is present.
    /// </summary>
    public static Stage Create(
        CompetitionId competitionId,
        StageName name,
        StageRegulation regulation,
        IClock clock) =>
        Create(
            competitionId,
            name,
            regulation,
            DefaultsBinding.AllBound(regulation.StandingRules is not null),
            StageId.New(),
            clock);

    /// <summary>
    /// Creates a new stage with an explicit defaults binding (MaterializeFrom + AllBound path).
    /// </summary>
    public static Stage Create(
        CompetitionId competitionId,
        StageName name,
        StageRegulation regulation,
        DefaultsBinding defaultsBinding,
        IClock clock) =>
        Create(competitionId, name, regulation, defaultsBinding, StageId.New(), clock);

    /// <summary>
    /// Creates a new stage in Draft status with an explicit identity and an independent copy of the given stage regulation.
    /// </summary>
    public static Stage Create(
        CompetitionId competitionId,
        StageName name,
        StageRegulation regulation,
        StageId id,
        IClock clock) =>
        Create(
            competitionId,
            name,
            regulation,
            DefaultsBinding.AllBound(regulation.StandingRules is not null),
            id,
            clock);

    /// <summary>
    /// Creates a new stage in Draft with explicit regulation and defaults binding.
    /// </summary>
    public static Stage Create(
        CompetitionId competitionId,
        StageName name,
        StageRegulation regulation,
        DefaultsBinding defaultsBinding,
        StageId id,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(regulation);
        ArgumentNullException.ThrowIfNull(defaultsBinding);
        ArgumentNullException.ThrowIfNull(clock);

        var stage = new Stage(id, competitionId, name, regulation.Copy(), defaultsBinding.Copy());
        stage.Raise(new StageCreated(stage.Id, competitionId, name.Value, clock));
        return stage;
    }

    /// <summary>
    /// Replaces the stage regulation as a whole (specialization). Unbinds heritable parts that change.
    /// Allowed in Draft or Ready; Ready is demoted to Draft.
    /// </summary>
    public void ReplaceRegulation(StageRegulation regulation, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(regulation);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();
        DemoteToDraftIfReady();

        UnbindChangedMatchParts(Regulation.MatchRules, regulation.MatchRules);
        UnbindChangedStandingParts(Regulation.StandingRules, regulation.StandingRules);

        Regulation = regulation.Copy();
        EnsureStandingRulesInvariant();
        Raise(new StageRegulationReplaced(Id, clock));
    }

    /// <summary>
    /// Replaces match rules (specialization). Unbinds each Match part whose value changes.
    /// Allowed in Draft or Ready; Ready is demoted to Draft.
    /// </summary>
    public void ReplaceMatchRules(MatchRules matchRules, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(matchRules);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();
        DemoteToDraftIfReady();

        UnbindChangedMatchParts(Regulation.MatchRules, matchRules);
        Regulation = Regulation.WithMatchRules(matchRules);
        Raise(new StageRegulationReplaced(Id, clock));
    }

    /// <summary>
    /// Replaces standing rules only. Allowed after Start (calculation ≠ structure).
    /// Specialization: unbinds Points and RankingCriteria.
    /// </summary>
    public void ReplaceStandingRules(StandingRules standingRules, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(standingRules);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStandingRulesMutable();

        if (Regulation.StandingRules is null || StageClassification.IsNonClassifyingPhase(this))
        {
            throw new DomainException(
                "Standing rules cannot be replaced on a non-classifying stage (StandingRules absent).",
                StageErrorCodes.StandingRulesInvariant);
        }

        DefaultsBinding = DefaultsBinding
            .Unbind(HeritableRegulationPart.Points)
            .Unbind(HeritableRegulationPart.RankingCriteria);

        Regulation = Regulation.WithStandingRules(standingRules);
        Raise(new StageStandingRulesReplaced(Id, clock));
    }

    /// <summary>
    /// Copies bound heritable parts from Competition defaults into this stage's effective regulation.
    /// Never mutates <see cref="DefaultsBinding"/>. No-op when status is not Draft or Ready.
    /// </summary>
    public void PropagateBoundDefaults(Regulation competitionRegulation, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competitionRegulation);
        ArgumentNullException.ThrowIfNull(clock);

        if (Status is not (StageStatus.Draft or StageStatus.Ready))
        {
            return;
        }

        var match = Regulation.MatchRules;
        var duration = DefaultsBinding.IsBound(HeritableRegulationPart.MatchDuration)
            ? CloneMatchDuration(competitionRegulation.MatchRules.Duration)
            : match.Duration;
        var administrative = DefaultsBinding.IsBound(HeritableRegulationPart.AdministrativeResult)
            ? CloneAdministrative(competitionRegulation.MatchRules.AdministrativeResultPolicy)
            : match.AdministrativeResultPolicy;
        var extraTime = DefaultsBinding.IsBound(HeritableRegulationPart.ExtraTime)
            ? CloneExtraTime(competitionRegulation.MatchRules.ExtraTimePolicy)
            : match.ExtraTimePolicy;
        var shootout = DefaultsBinding.IsBound(HeritableRegulationPart.PenaltyShootout)
            ? CloneShootout(competitionRegulation.MatchRules.PenaltyShootoutPolicy)
            : match.PenaltyShootoutPolicy;

        var nextMatch = new MatchRules(duration, administrative, extraTime, shootout);
        var nextStanding = Regulation.StandingRules;

        if (nextStanding is not null)
        {
            var points = DefaultsBinding.IsBound(HeritableRegulationPart.Points)
                ? ClonePoints(competitionRegulation.StandingRules.Points)
                : nextStanding.Points;
            var criteria = DefaultsBinding.IsBound(HeritableRegulationPart.RankingCriteria)
                ? competitionRegulation.StandingRules.RankingCriteria
                : nextStanding.RankingCriteria;
            nextStanding = new StandingRules(points, criteria);
        }

        var previous = Regulation;
        Regulation = previous.WithMatchRules(nextMatch);
        if (nextStanding is not null)
        {
            Regulation = Regulation.WithStandingRules(nextStanding);
        }

        if (!Regulation.Equals(previous))
        {
            Raise(new StageRegulationReplaced(Id, clock));
        }
    }

    /// <summary>
    /// Rebinds a heritable part to Competition defaults (copies value + marks bound).
    /// Domain primitive — no Host/SPA surface in Lot 2.
    /// </summary>
    public void BindToCompetition(HeritableRegulationPart part, Regulation competitionRegulation, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(competitionRegulation);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();

        switch (part)
        {
            case HeritableRegulationPart.MatchDuration:
                Regulation = Regulation.WithMatchRules(
                    new MatchRules(
                        CloneMatchDuration(competitionRegulation.MatchRules.Duration),
                        Regulation.MatchRules.AdministrativeResultPolicy,
                        Regulation.MatchRules.ExtraTimePolicy,
                        Regulation.MatchRules.PenaltyShootoutPolicy));
                break;
            case HeritableRegulationPart.ExtraTime:
                Regulation = Regulation.WithMatchRules(
                    new MatchRules(
                        Regulation.MatchRules.Duration,
                        Regulation.MatchRules.AdministrativeResultPolicy,
                        CloneExtraTime(competitionRegulation.MatchRules.ExtraTimePolicy),
                        Regulation.MatchRules.PenaltyShootoutPolicy));
                break;
            case HeritableRegulationPart.PenaltyShootout:
                Regulation = Regulation.WithMatchRules(
                    new MatchRules(
                        Regulation.MatchRules.Duration,
                        Regulation.MatchRules.AdministrativeResultPolicy,
                        Regulation.MatchRules.ExtraTimePolicy,
                        CloneShootout(competitionRegulation.MatchRules.PenaltyShootoutPolicy)));
                break;
            case HeritableRegulationPart.AdministrativeResult:
                Regulation = Regulation.WithMatchRules(
                    new MatchRules(
                        Regulation.MatchRules.Duration,
                        CloneAdministrative(competitionRegulation.MatchRules.AdministrativeResultPolicy),
                        Regulation.MatchRules.ExtraTimePolicy,
                        Regulation.MatchRules.PenaltyShootoutPolicy));
                break;
            case HeritableRegulationPart.Points:
                EnsureStandingPresentForBind();
                Regulation = Regulation.WithStandingRules(
                    new StandingRules(
                        ClonePoints(competitionRegulation.StandingRules.Points),
                        [.. Regulation.StandingRules!.RankingCriteria]));
                break;
            case HeritableRegulationPart.RankingCriteria:
                EnsureStandingPresentForBind();
                Regulation = Regulation.WithStandingRules(
                    new StandingRules(
                        Regulation.StandingRules!.Points,
                        [.. competitionRegulation.StandingRules.RankingCriteria]));
                break;
            default:
                throw new DomainException(
                    $"Unknown heritable regulation part '{part}'.",
                    StageErrorCodes.InvalidTransition);
        }

        DefaultsBinding = DefaultsBinding.Bind(part);
        Raise(new StageRegulationReplaced(Id, clock));
    }

    /// <summary>
    /// Clears standing rules (Cup/KO alignment). Allowed in Draft or Ready; Ready is demoted to Draft.
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    public void ClearStandingRules(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();
        DemoteToDraftIfReady();

        if (Regulation.StandingRules is null)
        {
            return;
        }

        Regulation = Regulation.WithStandingRules(null);
        EnsureStandingRulesInvariant();
        Raise(new StageRegulationReplaced(Id, clock));
    }

    /// <summary>
    /// Seeds standing rules from competition defaults when absent (classifying phases).
    /// Allowed in Draft or Ready; Ready is demoted to Draft. No-op when standing already present.
    /// </summary>
    /// <param name="standingDefaults">Competition standing defaults (A4 seed).</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void SeedStandingRules(StandingRules standingDefaults, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(standingDefaults);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();

        if (Regulation.StandingRules is not null)
        {
            return;
        }

        DemoteToDraftIfReady();
        Regulation = Regulation.WithStandingRules(standingDefaults);
        EnsureStandingRulesInvariant();
        Raise(new StageRegulationReplaced(Id, clock));
    }

    /// <summary>
    /// Replaces draw rules. Allowed in Draft or Ready; Ready is demoted to Draft.
    /// </summary>
    /// <param name="drawRules">The new draw rules, or <see langword="null"/>.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void ReplaceDrawRules(DrawRules? drawRules, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();
        DemoteToDraftIfReady();

        Regulation = Regulation.WithDrawRules(drawRules);
        Raise(new StageRegulationReplaced(Id, clock));
    }

    /// <summary>
    /// Adds a standing points deduction for an entry (mutable until Completed).
    /// </summary>
    /// <param name="entryId">Targeted competition entry.</param>
    /// <param name="pointsDeducted">Points to deduct (&gt; 0).</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <param name="reason">Optional free-text reason (traceability only).</param>
    /// <returns>The created penalty.</returns>
    public Penalty AddPenalty(EntryId entryId, int pointsDeducted, IClock clock, string? reason = null)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsurePenaltiesMutable();

        var penalty = new Penalty(PenaltyId.New(), entryId, pointsDeducted, reason);
        _penalties.Add(penalty);
        Raise(new StagePenaltyAdded(Id, penalty.Id, penalty.EntryId, penalty.PointsDeducted, penalty.Reason, clock));
        return penalty;
    }

    /// <summary>
    /// Removes a standing penalty from this stage (delete revocation).
    /// </summary>
    /// <param name="penaltyId">Penalty identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void RemovePenalty(PenaltyId penaltyId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsurePenaltiesMutable();

        var penalty = FindPenalty(penaltyId)
            ?? throw new DomainException(
                $"Penalty '{penaltyId}' was not found.",
                StageErrorCodes.PenaltyNotFound);

        _penalties.Remove(penalty);
        Raise(new StagePenaltyRemoved(Id, penalty.Id, penalty.EntryId, penalty.PointsDeducted, clock));
    }

    /// <summary>
    /// Finds a penalty by identity, or <see langword="null"/> when absent.
    /// </summary>
    public Penalty? FindPenalty(PenaltyId penaltyId) =>
        _penalties.FirstOrDefault(p => p.Id.Equals(penaltyId));

    /// <summary>
    /// Creates a draft draw with the given resolution kind (inputs configured separately).
    /// </summary>
    /// <param name="kind">Principal resolution kind (immutable for this draw).</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <returns>The created draw.</returns>
    public Draw CreateDraw(DrawResolutionKind kind, IClock clock) =>
        CreateDraw(kind, DrawId.New(), clock);

    /// <summary>
    /// Creates a draft draw with an explicit identity.
    /// </summary>
    /// <param name="kind">Principal resolution kind (immutable for this draw).</param>
    /// <param name="id">The draw identity (must not be empty).</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <returns>The created draw.</returns>
    public Draw CreateDraw(DrawResolutionKind kind, DrawId id, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();
        DemoteToDraftIfReady();

        var draw = new Draw(id, kind);
        _draws.Add(draw);
        Raise(new StageDrawCreated(Id, draw.Id, kind, clock));
        return draw;
    }

    /// <summary>
    /// Configures draw inputs (Draft draw only). Sole entry point for Entries / SeedMap / Pot / fixed placements.
    /// </summary>
    public void ConfigureDrawInputs(DrawId drawId, DrawInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        EnsureDraftOrReady();
        DemoteToDraftIfReady();
        GetDraw(drawId).ConfigureInputs(inputs);
    }

    /// <summary>
    /// Records a typed resolution matching the draw kind (Draft only).
    /// </summary>
    public void RecordDrawResolution(DrawId drawId, DrawResolution resolution, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();
        DemoteToDraftIfReady();

        var draw = GetDraw(drawId);
        draw.RecordResolution(resolution);
        Raise(new StageDrawResolutionRecorded(Id, drawId, resolution.State, clock));
    }

    /// <summary>
    /// Marks that no admissible solution exists (Draft only; ≠ Cancel).
    /// </summary>
    public void MarkDrawNoSolution(DrawId drawId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();
        DemoteToDraftIfReady();

        var draw = GetDraw(drawId);
        draw.MarkNoSolution();
        Raise(new StageDrawResolutionRecorded(Id, drawId, DrawResolutionState.NoSolution, clock));
    }

    /// <summary>
    /// Publishes a resolved draw (immutable thereafter). Required before WhoFeeds exposes Draw targets.
    /// </summary>
    public void PublishDraw(DrawId drawId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();

        var draw = GetDraw(drawId);
        draw.Publish();
        Raise(new StageDrawPublished(Id, drawId, clock));
    }

    /// <summary>
    /// Cancels a draw. A new draw is required for a rerun.
    /// </summary>
    public void CancelDraw(DrawId drawId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();
        DemoteToDraftIfReady();

        var draw = GetDraw(drawId);
        if (draw.Status == DrawStatus.Cancelled)
        {
            return;
        }

        draw.Cancel();
        Raise(new StageDrawCancelled(Id, drawId, clock));
    }

    /// <summary>
    /// Replaces qualification rules. Allowed in Draft or Ready; Ready is demoted to Draft.
    /// Local destinations (<see cref="QualificationDestination.StageId"/> equals this stage)
    /// must reference an existing slot and must not conflict with a direct assignment.
    /// </summary>
    /// <param name="qualificationRules">The new qualification rules, or <see langword="null"/>.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void ReplaceQualificationRules(QualificationRules? qualificationRules, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();

        if (qualificationRules is not null)
        {
            foreach (var path in qualificationRules.Paths)
            {
                EnsureLocalPathDestination(path.Destination.StageId, path.Destination.SlotKey);
            }
        }

        DemoteToDraftIfReady();
        Regulation = Regulation.WithQualificationRules(qualificationRules);
        Raise(new StageRegulationReplaced(Id, clock));
    }

    /// <summary>
    /// Replaces progression rules. Allowed in Draft or Ready; Ready is demoted to Draft.
    /// Each path fixture must belong to this stage.
    /// Local destinations must reference an existing slot and must not conflict with a direct assignment.
    /// </summary>
    /// <param name="progressionRules">The new progression rules, or <see langword="null"/>.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void ReplaceProgressionRules(ProgressionRules? progressionRules, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();

        if (progressionRules is not null)
        {
            foreach (var path in progressionRules.Paths)
            {
                if (!HasFixture(path.SourceFixtureId))
                {
                    throw new DomainException(
                        $"Fixture '{path.SourceFixtureId}' was not found.",
                        StageErrorCodes.FixtureNotFound);
                }

                if (path.Destination.TargetsPopulation)
                {
                    if (path.Destination.StageId.Equals(Id))
                    {
                        throw new DomainException(
                            "Progression population destination cannot target the source stage.",
                            RulesErrorCodes.ProgressionRulesInvalid);
                    }

                    continue;
                }

                EnsureLocalPathDestination(path.Destination.StageId, path.Destination.SlotKey!);
            }
        }

        DemoteToDraftIfReady();
        Regulation = Regulation.WithProgressionRules(progressionRules);
        Raise(new StageRegulationReplaced(Id, clock));
    }

    /// <summary>
    /// Replaces placement award rules. Allowed in Draft or Ready; Ready is demoted to Draft.
    /// Each path fixture must belong to this stage.
    /// Distinct from <see cref="ReplaceProgressionRules"/> — awards final ranks, does not feed slots.
    /// </summary>
    /// <param name="placementAwardRules">The new placement award rules, or <see langword="null"/>.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void ReplacePlacementAwardRules(PlacementAwardRules? placementAwardRules, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();

        if (placementAwardRules is not null)
        {
            foreach (var path in placementAwardRules.Paths)
            {
                if (!HasFixture(path.SourceFixtureId))
                {
                    throw new DomainException(
                        $"Fixture '{path.SourceFixtureId}' was not found.",
                        StageErrorCodes.FixtureNotFound);
                }
            }
        }

        DemoteToDraftIfReady();
        Regulation = Regulation.WithPlacementAwardRules(placementAwardRules);
        Raise(new StageRegulationReplaced(Id, clock));
    }

    /// <summary>
    /// Replaces the default stage tie format (source for new rounds). Allowed in Draft or Ready.
    /// </summary>
    /// <param name="tieFormat">The new default tie format, or <see langword="null"/>.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void ReplaceDefaultTieFormat(TieFormat? tieFormat, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();
        DemoteToDraftIfReady();

        Regulation = Regulation.WithTieFormat(tieFormat);
        Raise(new StageRegulationReplaced(Id, clock));
    }

    /// <summary>
    /// Gets a group by identity.
    /// </summary>
    /// <param name="groupId">The group identity.</param>
    /// <returns>The group.</returns>
    public Group GetGroup(GroupId groupId) =>
        _groups.FirstOrDefault(g => g.Id.Equals(groupId))
        ?? throw new DomainException($"Group '{groupId}' was not found.", StageErrorCodes.GroupNotFound);

    /// <summary>
    /// Gets a draw by identity.
    /// </summary>
    /// <param name="drawId">The draw identity.</param>
    /// <returns>The draw.</returns>
    public Draw GetDraw(DrawId drawId) =>
        _draws.FirstOrDefault(d => d.Id.Equals(drawId))
        ?? throw new DomainException($"Draw '{drawId}' was not found.", StageErrorCodes.DrawNotFound);

    /// <summary>
    /// Finds a draw by identity, if any.
    /// </summary>
    public Draw? FindDraw(DrawId drawId) =>
        _draws.FirstOrDefault(d => d.Id.Equals(drawId));

    /// <summary>
    /// Finds a group by identity, if any.
    /// </summary>
    /// <param name="groupId">The group identity.</param>
    /// <returns>The group, or <see langword="null"/>.</returns>
    public Group? FindGroup(GroupId groupId) =>
        _groups.FirstOrDefault(g => g.Id.Equals(groupId));

    /// <summary>
    /// Determines whether a group is present.
    /// </summary>
    /// <param name="groupId">The group identity.</param>
    /// <returns><see langword="true"/> if present; otherwise, <see langword="false"/>.</returns>
    public bool HasGroup(GroupId groupId) => FindGroup(groupId) is not null;

    /// <summary>
    /// Determines whether a round is present.
    /// </summary>
    /// <param name="roundId">The round identity.</param>
    /// <returns><see langword="true"/> if present; otherwise, <see langword="false"/>.</returns>
    public bool HasRound(RoundId roundId) => _rounds.Any(r => r.Id.Equals(roundId));

    /// <summary>
    /// Determines whether a matchday is present.
    /// </summary>
    /// <param name="matchdayId">The matchday identity.</param>
    /// <returns><see langword="true"/> if present; otherwise, <see langword="false"/>.</returns>
    public bool HasMatchday(MatchdayId matchdayId) => _matchdays.Any(m => m.Id.Equals(matchdayId));

    /// <summary>
    /// Gets a fixture by identity.
    /// </summary>
    /// <param name="fixtureId">The fixture identity.</param>
    /// <returns>The fixture.</returns>
    public Fixture GetFixture(FixtureId fixtureId) =>
        FindFixture(fixtureId)
        ?? throw new DomainException($"Fixture '{fixtureId}' was not found.", StageErrorCodes.FixtureNotFound);

    /// <summary>
    /// Finds a fixture by identity, if any.
    /// </summary>
    /// <param name="fixtureId">The fixture identity.</param>
    /// <returns>The fixture, or <see langword="null"/>.</returns>
    public Fixture? FindFixture(FixtureId fixtureId) =>
        _rounds.Select(r => r.FindFixture(fixtureId)).FirstOrDefault(f => f is not null)
        ?? _matchdays.Select(m => m.FindFixture(fixtureId)).FirstOrDefault(f => f is not null);

    /// <summary>
    /// Finds the fixture that currently attaches <paramref name="matchId"/>, if any.
    /// </summary>
    /// <param name="matchId">The match identity.</param>
    /// <returns>The fixture identity, or <see langword="null"/>.</returns>
    public FixtureId? FindFixtureIdContainingMatch(MatchId matchId)
    {
        foreach (var fixture in EnumerateFixtures())
        {
            if (fixture.Contains(matchId))
            {
                return fixture.Id;
            }
        }

        return null;
    }

    /// <summary>
    /// Determines whether a fixture is present.
    /// </summary>
    /// <param name="fixtureId">The fixture identity.</param>
    /// <returns><see langword="true"/> if present; otherwise, <see langword="false"/>.</returns>
    public bool HasFixture(FixtureId fixtureId) => FindFixture(fixtureId) is not null;

    /// <summary>
    /// Determines whether a match identity is attached to any fixture in this stage.
    /// </summary>
    /// <param name="matchId">The match identity.</param>
    /// <returns><see langword="true"/> if attached; otherwise, <see langword="false"/>.</returns>
    public bool HasMatch(MatchId matchId) =>
        _rounds.SelectMany(r => r.Fixtures).Any(f => f.Contains(matchId))
        || _matchdays.SelectMany(m => m.Fixtures).Any(f => f.Contains(matchId));

    /// <summary>
    /// Renames the stage. No-op when the normalized name is unchanged.
    /// </summary>
    /// <param name="name">The new name.</param>
    public void Rename(StageName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        EnsureDraftOrReady();

        if (Name.Equals(name))
        {
            return;
        }

        Name = name;
    }

    /// <summary>
    /// Sets how Championship / Groups matches are generated. No-op when unchanged.
    /// </summary>
    /// <param name="format">The match generation format.</param>
    public void SetMatchGenerationFormat(MatchGenerationFormat format)
    {
        EnsureDraftOrReady();

        if (MatchGenerationFormat == format)
        {
            return;
        }

        if (!Enum.IsDefined(format))
        {
            throw new DomainException(
                $"Unknown match generation format '{format}'.",
                StageErrorCodes.InvalidMatchGenerationFormat);
        }

        MatchGenerationFormat = format;
    }

    /// <summary>
    /// Enables or updates Swiss Kind settings. Pass <see langword="null"/> to clear when history is empty.
    /// </summary>
    /// <param name="settings">Swiss settings, or <see langword="null"/> to clear.</param>
    public void SetSwissSettings(SwissSettings? settings)
    {
        EnsureDraftOrReady();

        if (Equals(SwissSettings, settings))
        {
            return;
        }

        if (settings is null)
        {
            if (_swissByeHistory.Count > 0)
            {
                throw new DomainException(
                    "Swiss settings cannot be cleared while bye history exists.",
                    StageErrorCodes.SwissSettingsInvalid);
            }

            SwissSettings = null;
            return;
        }

        EnsureSwissCompositionAllowed();
        var maxByeRound = _swissByeHistory.Count == 0
            ? 0
            : _swissByeHistory.Max(bye => bye.RoundIndex);
        if (settings.RoundCount < maxByeRound)
        {
            throw new DomainException(
                $"Swiss round count cannot be less than highest recorded bye round ({maxByeRound}).",
                StageErrorCodes.SwissSettingsInvalid);
        }

        SwissSettings = settings;
    }

    /// <summary>
    /// Records a Swiss bye for a round. Allowed while Running (GenerateNextRound); not a Fixture/Match.
    /// </summary>
    /// <param name="roundIndex">1-based Swiss round index.</param>
    /// <param name="entryId">Entry receiving the bye.</param>
    public void RecordSwissBye(int roundIndex, EntryId entryId)
    {
        EnsureSwissByeMutable();

        var settings = SwissSettings
            ?? throw new DomainException(
                "Swiss bye can only be recorded on a Swiss stage.",
                StageErrorCodes.SwissByeInvalid);

        if (roundIndex < 1 || roundIndex > settings.RoundCount)
        {
            throw new DomainException(
                $"Swiss bye round index must be between 1 and {settings.RoundCount}.",
                StageErrorCodes.SwissByeInvalid);
        }

        if (_swissByeHistory.Any(bye => bye.RoundIndex == roundIndex))
        {
            throw new DomainException(
                $"Swiss bye already recorded for round {roundIndex}.",
                StageErrorCodes.SwissByeInvalid);
        }

        _swissByeHistory.Add(new SwissBye(roundIndex, entryId));
    }

    /// <summary>
    /// Counts recorded byes for an entry (I6 pairing input).
    /// </summary>
    /// <param name="entryId">Entry identity.</param>
    /// <returns>Number of byes in history.</returns>
    public int CountSwissByes(EntryId entryId) =>
        _swissByeHistory.Count(bye => bye.EntryId.Equals(entryId));

    /// <summary>
    /// Adds the next Swiss Matchday (progressive round). Allowed while Running — does not unlock global structure.
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    /// <returns>The created matchday (number = previous max + 1).</returns>
    public Matchday AddSwissRoundMatchday(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureSwissProgressiveMutable();
        EnsureCanAddMatchday();

        var settings = SwissSettings!;
        var nextNumber = _matchdays.Count == 0 ? 1 : _matchdays.Max(matchday => matchday.Number) + 1;
        if (nextNumber > settings.RoundCount)
        {
            throw new DomainException(
                $"Swiss stage already reached RoundCount {settings.RoundCount}.",
                StageErrorCodes.SwissSettingsInvalid);
        }

        var matchday = new Matchday(MatchdayId.New(), nextNumber);
        _matchdays.Add(matchday);
        Raise(new StageMatchdayAdded(Id, matchday.Id, clock));
        return matchday;
    }

    /// <summary>
    /// Adds a Fixture under a Swiss Matchday while Running (GenerateNextRound).
    /// </summary>
    /// <param name="matchdayId">Target Swiss matchday.</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <returns>The created fixture.</returns>
    public Fixture AddSwissRoundFixture(MatchdayId matchdayId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureSwissProgressiveMutable();

        var matchday = _matchdays.FirstOrDefault(candidate => candidate.Id.Equals(matchdayId))
            ?? throw new DomainException(
                $"Matchday '{matchdayId}' was not found.",
                StageErrorCodes.MatchdayNotFound);

        var fixture = new Fixture(FixtureId.New(), slotAKey: null, slotBKey: null);
        matchday.AddFixture(fixture);
        Raise(new StageFixtureAdded(Id, fixture.Id, clock));
        return fixture;
    }

    /// <summary>
    /// Attaches a Match to a Swiss Fixture while Running (GenerateNextRound). LegIndex 1 for V1.
    /// </summary>
    /// <param name="fixtureId">Fixture identity.</param>
    /// <param name="matchId">Match identity.</param>
    /// <param name="legIndex">1-based leg index.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void AttachSwissRoundMatch(FixtureId fixtureId, MatchId matchId, int legIndex, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureSwissProgressiveMutable();

        var fixture = GetFixture(fixtureId);
        if (fixture.Contains(matchId))
        {
            return;
        }

        if (HasMatch(matchId))
        {
            throw new DomainException(
                $"Match '{matchId}' is already attached to another fixture.",
                StageErrorCodes.MatchAlreadyAttached);
        }

        fixture.AttachMatch(matchId, legIndex);
        Raise(new StageMatchAttached(Id, fixtureId, matchId, legIndex, clock));
    }

    /// <summary>
    /// Clears Swiss Kind settings and bye history (Structure reconfigure). Draft/Ready only.
    /// </summary>
    public void ClearSwissConfiguration()
    {
        EnsureDraftOrReady();
        _swissByeHistory.Clear();
        SwissSettings = null;
    }

    /// <summary>
    /// Sets or clears structural places-per-group (Groups form capacity). Draft/Ready only.
    /// Independent of <see cref="ReplaceDrawRules"/> — clearing Draw does not clear this value.
    /// </summary>
    /// <param name="placesPerGroup">Places per group (≥ 2), or <see langword="null"/> to clear.</param>
    public void SetPlacesPerGroup(int? placesPerGroup)
    {
        EnsureDraftOrReady();

        switch (placesPerGroup)
        {
            case null:
                PlacesPerGroup = null;
                return;
            case < 2:
                throw new DomainException(
                    "Places per group must be at least 2.",
                    StageErrorCodes.InvalidConfiguration);
            default:
                PlacesPerGroup = placesPerGroup;
                break;
        }
    }

    /// <summary>
    /// Adds a group to the stage.
    /// </summary>
    /// <param name="name">The group name.</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <returns>The created group.</returns>
    public Group AddGroup(string name, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();
        EnsureCanAddGroup();

        DemoteToDraftIfReady();
        var group = new Group(GroupId.New(), name);
        _groups.Add(group);
        Raise(new StageGroupAdded(Id, group.Id, clock));
        return group;
    }

    /// <summary>
    /// Removes a group from the stage.
    /// </summary>
    /// <param name="groupId">The group identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void RemoveGroup(GroupId groupId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();

        var group = GetGroup(groupId);
        DemoteToDraftIfReady();
        _groups.Remove(group);
        Raise(new StageGroupRemoved(Id, groupId, clock));
    }

    /// <summary>
    /// Renames a group. No-op when the normalized name is unchanged.
    /// </summary>
    /// <param name="groupId">The group identity.</param>
    /// <param name="name">The new name.</param>
    public void RenameGroup(GroupId groupId, string name)
    {
        EnsureDraftOrReady();
        GetGroup(groupId).Rename(name);
    }

    /// <summary>
    /// Assigns an entry to a group. Application validates that the entry belongs to the competition.
    /// </summary>
    /// <param name="groupId">The group identity.</param>
    /// <param name="entryId">The entry identity.</param>
    public void AssignEntryToGroup(GroupId groupId, EntryId entryId)
    {
        EnsureStructureMutable();

        var owningGroup = _groups.FirstOrDefault(g => g.Contains(entryId));
        if (owningGroup is not null)
        {
            if (owningGroup.Id.Equals(groupId))
            {
                return;
            }

            throw new DomainException(
                $"Entry '{entryId}' is already assigned to another group.",
                StageErrorCodes.DuplicateEntry);
        }

        var group = GetGroup(groupId);
        DemoteToDraftIfReady();
        group.Assign(entryId);
    }

    /// <summary>
    /// Removes an entry from a group.
    /// </summary>
    /// <param name="groupId">The group identity.</param>
    /// <param name="entryId">The entry identity.</param>
    public void RemoveEntryFromGroup(GroupId groupId, EntryId entryId)
    {
        EnsureStructureMutable();

        var group = GetGroup(groupId);
        if (!group.Contains(entryId))
        {
            throw new DomainException(
                $"Entry '{entryId}' was not found in group '{groupId}'.",
                StageErrorCodes.EntryNotFound);
        }

        DemoteToDraftIfReady();
        group.Remove(entryId);
    }

    /// <summary>
    /// Adds a positional slot. Allowed in Draft or Ready; Ready is demoted to Draft.
    /// </summary>
    /// <param name="slotKey">Business slot key unique within the stage.</param>
    /// <returns>The created slot.</returns>
    public Slot AddSlot(string slotKey)
    {
        EnsureStructureMutable();
        EnsureNotSwiss("Slots");

        var key = Slot.NormalizeKey(slotKey);
        if (_slots.Any(s => string.Equals(s.SlotKey, key, StringComparison.Ordinal)))
        {
            throw new DomainException(
                $"Slot key '{key}' already exists.",
                StageErrorCodes.DuplicateSlotKey);
        }

        DemoteToDraftIfReady();
        var slot = new Slot(key);
        _slots.Add(slot);
        return slot;
    }

    /// <summary>
    /// Removes a slot when it is not referenced by direct assignment, local progression,
    /// local qualification, or fixture slots.
    /// </summary>
    /// <param name="slotKey">The slot key to remove.</param>
    public void RemoveSlot(string slotKey)
    {
        EnsureStructureMutable();

        var key = Slot.NormalizeKey(slotKey);
        var slot = FindSlot(key)
            ?? throw new DomainException($"Slot '{key}' was not found.", StageErrorCodes.SlotNotFound);

        if (_directAssignments.Any(a => string.Equals(a.SlotKey, key, StringComparison.Ordinal)))
        {
            throw new DomainException(
                $"Slot '{key}' is referenced by a direct assignment.",
                StageErrorCodes.SlotReferenced);
        }

        if (IsSlotReferencedByLocalProgression(key)
            || IsSlotReferencedByLocalQualification(key)
            || IsSlotReferencedByFixture(key))
        {
            throw new DomainException(
                $"Slot '{key}' is still referenced.",
                StageErrorCodes.SlotReferenced);
        }

        DemoteToDraftIfReady();
        _slots.Remove(slot);
    }

    /// <summary>
    /// Adds a resolved entry to the phase population (B1-M2 / Progression inter → population).
    /// Does not touch slots or <see cref="DirectAssignment"/>. Idempotent when already present.
    /// Allowed under the same mutability as dynamic slot resolution (Draft/Ready/Running/Suspended).
    /// </summary>
    /// <param name="entryId">Resolved competition entry.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void AddResolvedPopulationEntry(EntryId entryId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureResolutionMutable();

        if (_compositionEntries.Any(entry => entry.EntryId.Equals(entryId)))
        {
            return;
        }

        _compositionEntries.Add(new CompositionEntry(entryId));
        Raise(new StageCompositionEntriesReplaced(Id, clock));
    }

    /// <summary>
    /// Replaces the composition entry set (Affectation → Population). Allowed in Draft or Ready; Ready is demoted to Draft.
    /// Partial sets are allowed; duplicates are rejected. Order of first occurrence is preserved.
    /// </summary>
    /// <param name="entryIds">Entry identities (may be empty to clear).</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void ReplaceCompositionEntries(IReadOnlyList<EntryId> entryIds, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(entryIds);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();

        var distinct = new List<EntryId>(entryIds.Count);
        var seen = new HashSet<EntryId>();
        foreach (var entryId in entryIds)
        {
            if (!seen.Add(entryId))
            {
                throw new DomainException(
                    $"Entry '{entryId}' is duplicated in the composition set.",
                    StageErrorCodes.DuplicateEntry);
            }

            distinct.Add(entryId);
        }

        if (_compositionEntries.Count == distinct.Count
            && _compositionEntries.Select(entry => entry.EntryId).SequenceEqual(distinct))
        {
            return;
        }

        DemoteToDraftIfReady();
        _compositionEntries.Clear();
        foreach (var entryId in distinct)
        {
            _compositionEntries.Add(new CompositionEntry(entryId));
        }

        Raise(new StageCompositionEntriesReplaced(Id, clock));
    }

    /// <summary>
    /// Clears the root composition entry set (Structure rebuild). Draft/Ready only.
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    public void ClearCompositionEntries(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (_compositionEntries.Count == 0)
        {
            return;
        }

        ReplaceCompositionEntries([], clock);
    }

    /// <summary>
    /// Removes an entry from the composition set when present (e.g. hard-delete of a competition entry).
    /// </summary>
    /// <param name="entryId">Entry identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void RemoveCompositionEntryIfPresent(EntryId entryId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (_compositionEntries.All(entry => !entry.EntryId.Equals(entryId)))
        {
            return;
        }

        ReplaceCompositionEntries(
            [.. _compositionEntries.Where(entry => !entry.EntryId.Equals(entryId)).Select(entry => entry.EntryId)],
            clock);
    }

    /// <summary>
    /// Assigns an entry directly to a slot (configuration + synchronized resolution).
    /// </summary>
    /// <param name="slotKey">Target slot key.</param>
    /// <param name="entryId">Entry identity.</param>
    public void AssignEntryToSlot(string slotKey, EntryId entryId)
    {
        EnsureStructureMutable();

        var key = Slot.NormalizeKey(slotKey);
        var slot = FindSlot(key)
            ?? throw new DomainException($"Slot '{key}' was not found.", StageErrorCodes.SlotNotFound);

        if (IsSlotFedByLocalProgression(key))
        {
            throw new DomainException(
                $"Slot '{key}' already has a declarative progression feed.",
                StageErrorCodes.SlotFeedConflict);
        }

        if (IsSlotFedByLocalQualification(key))
        {
            throw new DomainException(
                $"Slot '{key}' already has a declarative qualification feed.",
                StageErrorCodes.SlotFeedConflict);
        }

        var occupyingSlot = _slots.FirstOrDefault(s => s.EntryId is { } occupied && occupied.Equals(entryId));
        if (occupyingSlot is not null && !string.Equals(occupyingSlot.SlotKey, key, StringComparison.Ordinal))
        {
            throw new DomainException(
                $"Entry '{entryId}' already occupies slot '{occupyingSlot.SlotKey}'.",
                StageErrorCodes.DuplicateEntry);
        }

        DemoteToDraftIfReady();

        var existingIndex = _directAssignments.FindIndex(a => string.Equals(a.SlotKey, key, StringComparison.Ordinal));
        var assignment = new DirectAssignment(key, entryId);
        if (existingIndex >= 0)
        {
            _directAssignments[existingIndex] = assignment;
        }
        else
        {
            _directAssignments.Add(assignment);
        }

        slot.SetEntry(entryId);
    }

    /// <summary>
    /// Clears a direct assignment and the slot's resolved entry.
    /// </summary>
    /// <param name="slotKey">Target slot key.</param>
    public void ClearSlotAssignment(string slotKey)
    {
        EnsureStructureMutable();

        var key = Slot.NormalizeKey(slotKey);
        var slot = FindSlot(key)
            ?? throw new DomainException($"Slot '{key}' was not found.", StageErrorCodes.SlotNotFound);

        var index = _directAssignments.FindIndex(a => string.Equals(a.SlotKey, key, StringComparison.Ordinal));
        if (index < 0)
        {
            // Direct clear only — do not wipe EntryId resolved by future Appliers / Draw.
            return;
        }

        DemoteToDraftIfReady();
        _directAssignments.RemoveAt(index);
        slot.ClearEntry();
    }

    /// <summary>
    /// Resolves a slot occupant dynamically without creating or modifying a <see cref="DirectAssignment"/>.
    /// </summary>
    /// <remarks>
    /// Resolution mutation (not structure): allowed in Draft, Ready, Running, and Suspended; forbidden when Completed.
    /// Does not demote Ready to Draft. A DirectAssignment on the target slot yields <see cref="StageErrorCodes.SlotFeedConflict"/>.
    /// When the entry already occupies another slot of this stage, it is moved (previous slot cleared without a separate event).
    /// </remarks>
    /// <param name="slotKey">Target slot key.</param>
    /// <param name="entryId">Resolved entry identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void ApplyResolvedEntry(string slotKey, EntryId entryId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureResolutionMutable();

        var key = Slot.NormalizeKey(slotKey);
        var slot = FindSlot(key)
            ?? throw new DomainException($"Slot '{key}' was not found.", StageErrorCodes.SlotNotFound);

        if (_directAssignments.Exists(a => string.Equals(a.SlotKey, key, StringComparison.Ordinal)))
        {
            throw new DomainException(
                $"Slot '{key}' is owned by a direct assignment and cannot receive a dynamic resolution.",
                StageErrorCodes.SlotFeedConflict);
        }

        if (slot.EntryId is { } current && current.Equals(entryId))
        {
            return;
        }

        foreach (var other in _slots.Where(other => !ReferenceEquals(other, slot)))
        {
            if (other.EntryId is { } occupied && occupied.Equals(entryId))
            {
                other.ClearEntry();
            }
        }

        var previousEntryId = slot.EntryId;
        slot.SetEntry(entryId);
        Raise(new StageSlotOccupantChanged(Id, key, previousEntryId, entryId, clock));
    }

    /// <summary>
    /// Clears a dynamically resolved slot occupant without affecting <see cref="DirectAssignment"/>.
    /// </summary>
    /// <remarks>
    /// Same mutability rules as <see cref="ApplyResolvedEntry"/>. No-op when already vacant.
    /// A DirectAssignment on the target slot yields <see cref="StageErrorCodes.SlotFeedConflict"/>.
    /// </remarks>
    /// <param name="slotKey">Target slot key.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void ClearResolvedEntry(string slotKey, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureResolutionMutable();

        var key = Slot.NormalizeKey(slotKey);
        var slot = FindSlot(key)
            ?? throw new DomainException($"Slot '{key}' was not found.", StageErrorCodes.SlotNotFound);

        if (_directAssignments.Exists(a => string.Equals(a.SlotKey, key, StringComparison.Ordinal)))
        {
            throw new DomainException(
                $"Slot '{key}' is owned by a direct assignment and cannot receive a dynamic resolution.",
                StageErrorCodes.SlotFeedConflict);
        }

        if (slot.EntryId is null)
        {
            return;
        }

        var previousEntryId = slot.EntryId;
        slot.ClearEntry();
        Raise(new StageSlotOccupantChanged(Id, key, previousEntryId, entryId: null, clock));
    }

    /// <summary>
    /// Finds a slot by key.
    /// </summary>
    /// <param name="slotKey">The slot key.</param>
    /// <returns>The slot, or <see langword="null"/>.</returns>
    public Slot? FindSlot(string slotKey)
    {
        var key = Slot.NormalizeKey(slotKey);
        return _slots.FirstOrDefault(s => string.Equals(s.SlotKey, key, StringComparison.Ordinal));
    }

    /// <summary>
    /// Arranges groups in the given order. No-op when the order is unchanged.
    /// </summary>
    /// <param name="orderedGroupIds">A permutation of current group identities.</param>
    public void ArrangeGroups(IReadOnlyList<GroupId> orderedGroupIds)
    {
        ArgumentNullException.ThrowIfNull(orderedGroupIds);
        EnsureStructureMutable();
        EnsurePermutation(orderedGroupIds, _groups.ConvertAll(g => g.Id));

        if (_groups.Select(g => g.Id).SequenceEqual(orderedGroupIds))
        {
            return;
        }

        DemoteToDraftIfReady();
        var map = _groups.ToDictionary(g => g.Id);
        _groups.Clear();
        foreach (var id in orderedGroupIds)
        {
            _groups.Add(map[id]);
        }
    }

    /// <summary>
    /// Adds a round to the stage, materializing <see cref="StageRegulation.TieFormat"/> when present.
    /// </summary>
    /// <param name="name">The round name.</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <returns>The created round.</returns>
    public Round AddRound(string name, IClock clock) =>
        AddRound(name, Regulation.TieFormat, clock);

    /// <summary>
    /// Adds a round with an explicit tie format (or <see langword="null"/> to omit one).
    /// </summary>
    /// <param name="name">The round name.</param>
    /// <param name="tieFormat">The tie format to materialize, or <see langword="null"/>.</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <returns>The created round.</returns>
    public Round AddRound(string name, TieFormat? tieFormat, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();
        EnsureCanAddRound();

        DemoteToDraftIfReady();
        var round = new Round(RoundId.New(), name, tieFormat?.Copy());
        _rounds.Add(round);

        // Becoming knockout (A5): standing capacity is removed with the topology change.
        if (Regulation.StandingRules is not null)
        {
            Regulation = Regulation.WithStandingRules(null);
            Raise(new StageRegulationReplaced(Id, clock));
        }

        EnsureStandingRulesInvariant();
        Raise(new StageRoundAdded(Id, round.Id, clock));
        return round;
    }

    /// <summary>
    /// Replaces the tie format of a round. Allowed before the stage structure is locked.
    /// </summary>
    /// <param name="roundId">The round identity.</param>
    /// <param name="tieFormat">The new tie format, or <see langword="null"/>.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void ReplaceRoundTieFormat(RoundId roundId, TieFormat? tieFormat, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();

        var round = _rounds.FirstOrDefault(r => r.Id.Equals(roundId))
            ?? throw new DomainException($"Round '{roundId}' was not found.", StageErrorCodes.RoundNotFound);

        DemoteToDraftIfReady();
        round.ReplaceTieFormat(tieFormat?.Copy());
        Raise(new StageRoundTieFormatReplaced(Id, roundId, clock));
    }

    /// <summary>
    /// Removes a round from the stage.
    /// </summary>
    /// <param name="roundId">The round identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void RemoveRound(RoundId roundId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();

        var round = _rounds.FirstOrDefault(r => r.Id.Equals(roundId))
            ?? throw new DomainException($"Round '{roundId}' was not found.", StageErrorCodes.RoundNotFound);

        DemoteToDraftIfReady();
        _rounds.Remove(round);
        Raise(new StageRoundRemoved(Id, roundId, clock));
    }

    /// <summary>
    /// Renames a round. No-op when the normalized name is unchanged.
    /// </summary>
    /// <param name="roundId">The round identity.</param>
    /// <param name="name">The new name.</param>
    public void RenameRound(RoundId roundId, string name)
    {
        EnsureDraftOrReady();

        var round = _rounds.FirstOrDefault(r => r.Id.Equals(roundId))
            ?? throw new DomainException($"Round '{roundId}' was not found.", StageErrorCodes.RoundNotFound);
        round.Rename(name);
    }

    /// <summary>
    /// Arranges rounds in the given order. No-op when the order is unchanged.
    /// </summary>
    /// <param name="orderedRoundIds">A permutation of current round identities.</param>
    public void ArrangeRounds(IReadOnlyList<RoundId> orderedRoundIds)
    {
        ArgumentNullException.ThrowIfNull(orderedRoundIds);
        EnsureStructureMutable();
        EnsurePermutation(orderedRoundIds, _rounds.ConvertAll(r => r.Id));

        if (_rounds.Select(r => r.Id).SequenceEqual(orderedRoundIds))
        {
            return;
        }

        DemoteToDraftIfReady();
        var map = _rounds.ToDictionary(r => r.Id);
        _rounds.Clear();
        foreach (var id in orderedRoundIds)
        {
            _rounds.Add(map[id]);
        }
    }

    /// <summary>
    /// Adds a matchday to the stage.
    /// </summary>
    /// <param name="number">The 1-based matchday number.</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <returns>The created matchday.</returns>
    public Matchday AddMatchday(int number, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();
        EnsureCanAddMatchday();

        DemoteToDraftIfReady();
        var matchday = new Matchday(MatchdayId.New(), number);
        _matchdays.Add(matchday);
        Raise(new StageMatchdayAdded(Id, matchday.Id, clock));
        return matchday;
    }

    /// <summary>
    /// Removes a matchday from the stage.
    /// </summary>
    /// <param name="matchdayId">The matchday identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void RemoveMatchday(MatchdayId matchdayId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();

        var matchday = _matchdays.FirstOrDefault(m => m.Id.Equals(matchdayId))
            ?? throw new DomainException(
                $"Matchday '{matchdayId}' was not found.",
                StageErrorCodes.MatchdayNotFound);

        DemoteToDraftIfReady();
        _matchdays.Remove(matchday);
        Raise(new StageMatchdayRemoved(Id, matchdayId, clock));
    }

    /// <summary>
    /// Arranges matchdays in the given order. No-op when the order is unchanged.
    /// </summary>
    /// <param name="orderedMatchdayIds">A permutation of current matchday identities.</param>
    public void ArrangeMatchdays(IReadOnlyList<MatchdayId> orderedMatchdayIds)
    {
        ArgumentNullException.ThrowIfNull(orderedMatchdayIds);
        EnsureStructureMutable();
        EnsurePermutation(orderedMatchdayIds, _matchdays.ConvertAll(m => m.Id));

        if (_matchdays.Select(m => m.Id).SequenceEqual(orderedMatchdayIds))
        {
            return;
        }

        DemoteToDraftIfReady();
        var map = _matchdays.ToDictionary(m => m.Id);
        _matchdays.Clear();
        foreach (var id in orderedMatchdayIds)
        {
            _matchdays.Add(map[id]);
        }
    }

    /// <summary>
    /// Adds a fixture under a round.
    /// </summary>
    /// <param name="roundId">The round identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <param name="slotAKey">Optional bracket slot A.</param>
    /// <param name="slotBKey">Optional bracket slot B.</param>
    /// <returns>The created fixture.</returns>
    public Fixture AddFixture(RoundId roundId, IClock clock, string? slotAKey = null, string? slotBKey = null)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();

        var round = _rounds.FirstOrDefault(r => r.Id.Equals(roundId))
            ?? throw new DomainException($"Round '{roundId}' was not found.", StageErrorCodes.RoundNotFound);

        EnsureSlotKeysExist(slotAKey, slotBKey);
        DemoteToDraftIfReady();
        var fixture = new Fixture(FixtureId.New(), slotAKey, slotBKey);
        round.AddFixture(fixture);
        Raise(new StageFixtureAdded(Id, fixture.Id, clock));
        return fixture;
    }

    /// <summary>
    /// Adds a fixture under a matchday.
    /// </summary>
    /// <param name="matchdayId">The matchday identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <param name="slotAKey">Optional bracket slot A.</param>
    /// <param name="slotBKey">Optional bracket slot B.</param>
    /// <returns>The created fixture.</returns>
    public Fixture AddFixture(MatchdayId matchdayId, IClock clock, string? slotAKey = null, string? slotBKey = null)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();

        var matchday = _matchdays.FirstOrDefault(m => m.Id.Equals(matchdayId))
            ?? throw new DomainException(
                $"Matchday '{matchdayId}' was not found.",
                StageErrorCodes.MatchdayNotFound);

        EnsureSlotKeysExist(slotAKey, slotBKey);
        DemoteToDraftIfReady();
        var fixture = new Fixture(FixtureId.New(), slotAKey, slotBKey);
        matchday.AddFixture(fixture);
        Raise(new StageFixtureAdded(Id, fixture.Id, clock));
        return fixture;
    }

    /// <summary>
    /// Binds or clears bracket slot keys on a fixture.
    /// </summary>
    /// <param name="fixtureId">The fixture identity.</param>
    /// <param name="slotAKey">Optional bracket slot A.</param>
    /// <param name="slotBKey">Optional bracket slot B.</param>
    public void ReplaceFixtureSlots(FixtureId fixtureId, string? slotAKey, string? slotBKey)
    {
        EnsureStructureMutable();

        var fixture = GetFixture(fixtureId);
        EnsureSlotKeysExist(slotAKey, slotBKey);
        DemoteToDraftIfReady();
        fixture.BindSlots(slotAKey, slotBKey);
    }

    /// <summary>
    /// Removes a fixture from the stage.
    /// </summary>
    /// <param name="fixtureId">The fixture identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void RemoveFixture(FixtureId fixtureId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();
        var fixture = GetFixture(fixtureId);
        var matchIds = fixture.MatchIds.ToArray();

        DemoteToDraftIfReady();
        if (_rounds.Any(round => round.RemoveFixture(fixtureId)))
        {
            ClearMatchPlacements(matchIds);
            Raise(new StageFixtureRemoved(Id, fixtureId, clock));
            return;
        }

        if (!_matchdays.Any(matchday => matchday.RemoveFixture(fixtureId))) return;
        ClearMatchPlacements(matchIds);
        Raise(new StageFixtureRemoved(Id, fixtureId, clock));
    }

    /// <summary>
    /// Attaches a match identity to a fixture with an explicit leg index.
    /// No-op when already on that fixture.
    /// Application validates that the match belongs to this stage (<c>Match.StageId</c>).
    /// </summary>
    /// <param name="fixtureId">The fixture identity.</param>
    /// <param name="matchId">The match identity.</param>
    /// <param name="legIndex">1-based confrontation leg index.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void AttachMatch(FixtureId fixtureId, MatchId matchId, int legIndex, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();

        var fixture = GetFixture(fixtureId);
        if (fixture.Contains(matchId))
        {
            return;
        }

        if (HasMatch(matchId))
        {
            throw new DomainException(
                $"Match '{matchId}' is already attached to another fixture.",
                StageErrorCodes.MatchAlreadyAttached);
        }

        DemoteToDraftIfReady();
        fixture.AttachMatch(matchId, legIndex);
        Raise(new StageMatchAttached(Id, fixtureId, matchId, legIndex, clock));
    }

    /// <summary>
    /// Detaches a match identity from a fixture.
    /// </summary>
    /// <param name="fixtureId">The fixture identity.</param>
    /// <param name="matchId">The match identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void DetachMatch(FixtureId fixtureId, MatchId matchId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStructureMutable();

        var fixture = GetFixture(fixtureId);
        if (!fixture.Contains(matchId))
        {
            throw new DomainException(
                $"Match '{matchId}' is not attached to fixture '{fixtureId}'.",
                StageErrorCodes.MatchNotAttached);
        }

        DemoteToDraftIfReady();
        fixture.DetachMatch(matchId);
        ClearMatchPlacement(matchId);
        Raise(new StageMatchDetached(Id, fixtureId, matchId, clock));
    }

    /// <summary>
    /// Materializes calendar placements from a successful schedule resolution.
    /// Prefights all invariants, then upserts <paramref name="targets"/> atomically.
    /// Placements outside <paramref name="targets"/> (Fixed) are left unchanged;
    /// if present in <paramref name="placements"/> they must match the current Stage state.
    /// Idempotent when the same target placements are reapplied.
    /// Does not re-run scheduling constraints (authority remains <c>ScheduleGenerator</c>).
    /// </summary>
    /// <param name="placements">Assignments from a Success schedule (typically Fixed ∪ Targets).</param>
    /// <param name="targets">Match identities authorized for write/replace.</param>
    public void ApplyMatchPlacements(
        IReadOnlyList<MatchPlacement> placements,
        IReadOnlyList<MatchId> targets)
    {
        ArgumentNullException.ThrowIfNull(placements);
        ArgumentNullException.ThrowIfNull(targets);

        if (targets.Distinct().Count() != targets.Count)
        {
            throw new DomainException(
                "Target match identities cannot contain duplicates.",
                StageErrorCodes.MatchPlacementInvalid);
        }

        if (placements.Select(p => p.MatchId).Distinct().Count() != placements.Count)
        {
            throw new DomainException(
                "Schedule placements must have unique match identities.",
                StageErrorCodes.MatchPlacementInvalid);
        }

        var byMatchId = placements.ToDictionary(p => p.MatchId);
        var targetSet = targets.ToHashSet();

        foreach (var targetId in targets)
        {
            if (!byMatchId.ContainsKey(targetId))
            {
                throw new DomainException(
                    $"Target match '{targetId}' is missing from the schedule placements.",
                    StageErrorCodes.MatchPlacementInvalid);
            }

            if (!HasMatch(targetId))
            {
                throw new DomainException(
                    $"Match '{targetId}' is not attached to this stage.",
                    StageErrorCodes.MatchPlacementInvalid);
            }
        }

        foreach (var placement in placements)
        {
            if (targetSet.Contains(placement.MatchId))
            {
                continue;
            }

            // Fixed claim inside the schedule payload.
            if (!HasMatch(placement.MatchId))
            {
                throw new DomainException(
                    $"Fixed match '{placement.MatchId}' is not attached to this stage.",
                    StageErrorCodes.MatchPlacementInvalid);
            }

            var current = FindMatchPlacement(placement.MatchId);
            if (current is null
                || current.Start != placement.Start
                || !current.ResourceId.Equals(placement.ResourceId))
            {
                throw new DomainException(
                    $"Fixed placement for match '{placement.MatchId}' diverges from the stage.",
                    StageErrorCodes.MatchPlacementInvalid);
            }
        }

        foreach (var targetId in targets)
        {
            UpsertMatchPlacement(byMatchId[targetId]);
        }
    }

    /// <summary>
    /// Tries to get the materialized placement for a match.
    /// </summary>
    public bool TryGetMatchPlacement(MatchId matchId, out MatchPlacement placement)
    {
        var found = FindMatchPlacement(matchId);
        if (found is null)
        {
            placement = null!;
            return false;
        }

        placement = found;
        return true;
    }

    private MatchPlacement? FindMatchPlacement(MatchId matchId) =>
        _matchPlacements.Find(p => p.MatchId.Equals(matchId));

    private void UpsertMatchPlacement(MatchPlacement placement)
    {
        var index = _matchPlacements.FindIndex(p => p.MatchId.Equals(placement.MatchId));
        if (index < 0)
        {
            _matchPlacements.Add(placement);
        }
        else
        {
            _matchPlacements[index] = placement;
        }
    }

    private void ClearMatchPlacement(MatchId matchId)
    {
        var index = _matchPlacements.FindIndex(p => p.MatchId.Equals(matchId));
        if (index >= 0)
        {
            _matchPlacements.RemoveAt(index);
        }
    }

    private void ClearMatchPlacements(IEnumerable<MatchId> matchIds)
    {
        foreach (var matchId in matchIds)
        {
            ClearMatchPlacement(matchId);
        }
    }

    /// <summary>
    /// Prepares the stage for its current abstraction level (Draft to Ready).
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    public void Prepare(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStatus(StageStatus.Draft, "Stage can only be prepared from Draft.");

        if (!HasStructure && !IsSwiss)
        {
            throw new DomainException(
                "Stage requires structure before Prepare.",
                StageErrorCodes.NotReady);
        }

        if (IsSwiss && SwissSettings is null)
        {
            throw new DomainException(
                "Swiss stage requires SwissSettings before Prepare.",
                StageErrorCodes.NotReady);
        }

        // Elimination (Phase 5.5): ≥1 Round is enough; fixtures optional for Prepare.
        // Championship: Matchdays only. Poules: Groups + Matchdays + ≥1 entry.
        // Swiss: SwissSettings is enough — Matchdays are created by GenerateNextRound.
        if (_rounds.Count == 0 && _groups.Count > 0)
        {
            if (_matchdays.Count == 0)
            {
                throw new DomainException(
                    "Poule stage requires at least one matchday before Prepare.",
                    StageErrorCodes.InvalidConfiguration);
            }

            if (_groups.All(g => g.EntryIds.Count == 0))
            {
                throw new DomainException(
                    "Poule stage requires at least one group with an entry before Prepare.",
                    StageErrorCodes.InvalidConfiguration);
            }
        }

        EnsureLocalSlotConfigurationForPrepare();

        Status = StageStatus.Ready;
        Raise(new StagePrepared(Id, clock));
    }

    /// <summary>
    /// Starts the stage (Ready to Running).
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    public void Start(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStatus(StageStatus.Ready, "Stage can only be started from Ready.");
        EnsureInitialRoundPlayableWhenPositional();
        Status = StageStatus.Running;
        Raise(new StageStarted(Id, clock));
    }

    /// <summary>
    /// Suspends a running stage.
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    public void Suspend(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStatus(StageStatus.Running, "Stage can only be suspended from Running.");
        Status = StageStatus.Suspended;
        Raise(new StageSuspended(Id, clock));
    }

    /// <summary>
    /// Resumes a suspended stage.
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    public void Resume(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStatus(StageStatus.Suspended, "Stage can only be resumed from Suspended.");
        Status = StageStatus.Running;
        Raise(new StageResumed(Id, clock));
    }

    /// <summary>
    /// Completes the stage.
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    public void Complete(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (Status is not (StageStatus.Running or StageStatus.Suspended))
        {
            throw new DomainException(
                $"Stage cannot be completed from '{Status}'.",
                StageErrorCodes.InvalidTransition);
        }

        Status = StageStatus.Completed;
        Raise(new StageCompleted(Id, clock));
    }

    private static void EnsurePermutation<TId>(IReadOnlyList<TId> ordered, IReadOnlyList<TId> current)
        where TId : notnull
    {
        if (ordered.Count != current.Count
            || ordered.Distinct().Count() != ordered.Count
            || ordered.Any(id => !current.Contains(id)))
        {
            throw new DomainException(
                "Order must be a permutation of the current identities.",
                StageErrorCodes.InvalidOrder);
        }
    }

    private static MatchDuration CloneMatchDuration(MatchDuration source) =>
        new(source.DurationPerPeriod, source.NumberOfPeriods, source.HalfTimeDuration);

    private static AdministrativeResultPolicy CloneAdministrative(AdministrativeResultPolicy source) =>
        new(source.ForfeitWinnerGoals, source.ForfeitLoserGoals);

    private static ExtraTimePolicy? CloneExtraTime(ExtraTimePolicy? source) =>
        source is null ? null : new ExtraTimePolicy(source.DurationPerPeriod, source.NumberOfPeriods);

    private static PenaltyShootoutPolicy? CloneShootout(PenaltyShootoutPolicy? source) =>
        source is null ? null : new PenaltyShootoutPolicy(source.InitialKicksPerTeam);

    private static PointsPolicy ClonePoints(PointsPolicy source) =>
        new(source.WinPoints, source.DrawPoints, source.LossPoints);

    private void DemoteToDraftIfReady()
    {
        if (Status == StageStatus.Ready)
        {
            Status = StageStatus.Draft;
        }
    }

    private void UnbindChangedMatchParts(MatchRules before, MatchRules after)
    {
        if (!before.Duration.Equals(after.Duration))
        {
            DefaultsBinding = DefaultsBinding.Unbind(HeritableRegulationPart.MatchDuration);
        }

        if (!Equals(before.ExtraTimePolicy, after.ExtraTimePolicy))
        {
            DefaultsBinding = DefaultsBinding.Unbind(HeritableRegulationPart.ExtraTime);
        }

        if (!Equals(before.PenaltyShootoutPolicy, after.PenaltyShootoutPolicy))
        {
            DefaultsBinding = DefaultsBinding.Unbind(HeritableRegulationPart.PenaltyShootout);
        }

        if (!before.AdministrativeResultPolicy.Equals(after.AdministrativeResultPolicy))
        {
            DefaultsBinding = DefaultsBinding.Unbind(HeritableRegulationPart.AdministrativeResult);
        }
    }

    private void UnbindChangedStandingParts(StandingRules? before, StandingRules? after)
    {
        if (before is null && after is null)
        {
            return;
        }

        if (before is null || after is null)
        {
            DefaultsBinding = DefaultsBinding
                .Unbind(HeritableRegulationPart.Points)
                .Unbind(HeritableRegulationPart.RankingCriteria);
            return;
        }

        if (!before.Points.Equals(after.Points))
        {
            DefaultsBinding = DefaultsBinding.Unbind(HeritableRegulationPart.Points);
        }

        if (!before.RankingCriteria.SequenceEqual(after.RankingCriteria))
        {
            DefaultsBinding = DefaultsBinding.Unbind(HeritableRegulationPart.RankingCriteria);
        }
    }

    private void EnsureStandingPresentForBind()
    {
        if (Regulation.StandingRules is null)
        {
            throw new DomainException(
                "Standing rules cannot be bound on a stage without StandingRules.",
                StageErrorCodes.StandingRulesInvariant);
        }
    }

    private void EnsureDraftOrReady()
    {
        if (Status is not (StageStatus.Draft or StageStatus.Ready))
        {
            throw new DomainException(
                $"Operation is not allowed when status is '{Status}'.",
                StageErrorCodes.InvalidTransition);
        }
    }

    private void EnsureStandingRulesMutable()
    {
        if (Status is StageStatus.Completed)
        {
            throw new DomainException(
                $"Standing rules cannot be modified when status is '{Status}'.",
                StageErrorCodes.InvalidTransition);
        }
    }

    /// <summary>
    /// Enforces A5 against current topology: classifying ⇒ Standing present; knockout ⇒ Standing absent.
    /// Unstructured stages impose no presence constraint (seed may exist before ConfigureStructure).
    /// </summary>
    private void EnsureStandingRulesInvariant()
    {
        if (StageClassification.IsClassifyingPhase(this) && Regulation.StandingRules is null)
        {
            throw new DomainException(
                "A classifying stage must have StandingRules.",
                StageErrorCodes.StandingRulesInvariant);
        }

        if (StageClassification.IsNonClassifyingPhase(this) && Regulation.StandingRules is not null)
        {
            throw new DomainException(
                "A non-classifying (knockout) stage must not have StandingRules.",
                StageErrorCodes.StandingRulesInvariant);
        }
    }

    private void EnsurePenaltiesMutable()
    {
        if (Status is StageStatus.Completed)
        {
            throw new DomainException(
                $"Penalties cannot be modified when status is '{Status}'.",
                StageErrorCodes.InvalidTransition);
        }
    }

    private void EnsureResolutionMutable()
    {
        if (Status is StageStatus.Completed)
        {
            throw new DomainException(
                $"Slot occupant cannot be resolved when status is '{Status}'.",
                StageErrorCodes.InvalidTransition);
        }
    }

    private void EnsureStructureMutable()
    {
        if (Status is StageStatus.Running or StageStatus.Suspended or StageStatus.Completed)
        {
            throw new DomainException(
                $"Structure cannot be modified when status is '{Status}'.",
                StageErrorCodes.StructureLocked);
        }
    }

    private void EnsureCanAddGroup()
    {
        EnsureNotSwiss("Groups");
        if (_rounds.Count > 0)
        {
            throw new DomainException(
                "Groups cannot be combined with rounds.",
                StageErrorCodes.InvalidComposition);
        }
    }

    private void EnsureCanAddRound()
    {
        EnsureNotSwiss("Rounds");
        if (_groups.Count > 0)
        {
            throw new DomainException(
                "Rounds cannot be combined with groups.",
                StageErrorCodes.InvalidComposition);
        }

        if (_matchdays.Count > 0)
        {
            throw new DomainException(
                "Rounds cannot be combined with matchdays.",
                StageErrorCodes.InvalidComposition);
        }
    }

    private void EnsureCanAddMatchday()
    {
        if (_rounds.Count > 0)
        {
            throw new DomainException(
                "Matchdays cannot be combined with rounds.",
                StageErrorCodes.InvalidComposition);
        }
    }

    private void EnsureSwissCompositionAllowed()
    {
        if (_groups.Count > 0 || _rounds.Count > 0 || _slots.Count > 0)
        {
            throw new DomainException(
                "Swiss settings require Matchdays-only composition (no Groups, Rounds, or Slots).",
                StageErrorCodes.InvalidComposition);
        }
    }

    private void EnsureNotSwiss(string composition)
    {
        if (!IsSwiss)
        {
            return;
        }

        throw new DomainException(
            $"{composition} cannot be combined with Swiss settings.",
            StageErrorCodes.InvalidComposition);
    }

    private void EnsureSwissByeMutable()
    {
        if (Status is StageStatus.Completed)
        {
            throw new DomainException(
                $"Swiss bye cannot be recorded when status is '{Status}'.",
                StageErrorCodes.InvalidTransition);
        }
    }

    /// <summary>
    /// Swiss progressive round materialization: Draft/Ready/Running, not Suspended/Completed.
    /// Does not call <see cref="EnsureStructureMutable"/> (StructureLocked stays for non-Swiss ops).
    /// </summary>
    private void EnsureSwissProgressiveMutable()
    {
        if (!IsSwiss)
        {
            throw new DomainException(
                "Swiss progressive round APIs require Swiss settings.",
                StageErrorCodes.SwissSettingsInvalid);
        }

        if (Status is StageStatus.Suspended or StageStatus.Completed)
        {
            throw new DomainException(
                $"Swiss round cannot be materialized when status is '{Status}'.",
                StageErrorCodes.InvalidTransition);
        }
    }

    private void EnsureStatus(StageStatus expected, string message)
    {
        if (Status != expected)
        {
            throw new DomainException(message, StageErrorCodes.InvalidTransition);
        }
    }

    private bool IsSlotFedByLocalProgression(string slotKey) =>
        Regulation.ProgressionRules?.Paths.Any(p =>
            p.Destination.StageId.Equals(Id)
            && string.Equals(p.Destination.SlotKey, slotKey, StringComparison.Ordinal))
        == true;

    private bool IsSlotFedByLocalQualification(string slotKey) =>
        Regulation.QualificationRules?.Paths.Any(p =>
            p.Destination.StageId.Equals(Id)
            && string.Equals(p.Destination.SlotKey, slotKey, StringComparison.Ordinal))
        == true;

    private bool IsSlotReferencedByLocalProgression(string slotKey) =>
        IsSlotFedByLocalProgression(slotKey);

    private bool IsSlotReferencedByLocalQualification(string slotKey) =>
        IsSlotFedByLocalQualification(slotKey);

    private bool IsSlotReferencedByFixture(string slotKey) =>
        EnumerateFixtures().Any(f =>
            string.Equals(f.SlotAKey, slotKey, StringComparison.Ordinal)
            || string.Equals(f.SlotBKey, slotKey, StringComparison.Ordinal));

    private IEnumerable<Fixture> EnumerateFixtures() =>
        _rounds.SelectMany(r => r.Fixtures).Concat(_matchdays.SelectMany(m => m.Fixtures));

    private void EnsureSlotKeysExist(string? slotAKey, string? slotBKey)
    {
        if (slotAKey is not null && FindSlot(slotAKey) is null)
        {
            throw new DomainException(
                $"Slot '{Slot.NormalizeKey(slotAKey)}' was not found.",
                StageErrorCodes.SlotNotFound);
        }

        if (slotBKey is not null && FindSlot(slotBKey) is null)
        {
            throw new DomainException(
                $"Slot '{Slot.NormalizeKey(slotBKey)}' was not found.",
                StageErrorCodes.SlotNotFound);
        }
    }

    /// <summary>
    /// Validates local destination for Qualification / Progression paths that target this stage.
    /// Cross-stage destinations are validated by Application <c>PrepareStage</c>.
    /// </summary>
    private void EnsureLocalPathDestination(StageId destinationStageId, string slotKey)
    {
        if (!destinationStageId.Equals(Id))
        {
            return;
        }

        if (FindSlot(slotKey) is null)
        {
            throw new DomainException(
                $"Slot '{slotKey}' was not found.",
                StageErrorCodes.SlotNotFound);
        }

        if (_directAssignments.Any(a => string.Equals(a.SlotKey, slotKey, StringComparison.Ordinal)))
        {
            throw new DomainException(
                $"Slot '{slotKey}' already has a direct assignment feed.",
                StageErrorCodes.SlotFeedConflict);
        }
    }

    /// <summary>
    /// Validates local slot/fixture/direct/progression/qualification consistency for Prepare.
    /// Does not require a global feed (inbound Qualification may exist outside this aggregate).
    /// Rejects multiple local feeds on the same slot.
    /// </summary>
    private void EnsureLocalSlotConfigurationForPrepare()
    {
        if (Regulation.ProgressionRules is { } progression)
        {
            foreach (var path in progression.Paths)
            {
                if (!HasFixture(path.SourceFixtureId))
                {
                    throw new DomainException(
                        $"Fixture '{path.SourceFixtureId}' was not found.",
                        StageErrorCodes.FixtureNotFound);
                }

                if (path.Destination.TargetsPopulation)
                {
                    continue;
                }

                if (path.Destination.StageId.Equals(Id) && FindSlot(path.Destination.SlotKey!) is null)
                {
                    throw new DomainException(
                        $"Slot '{path.Destination.SlotKey}' was not found.",
                        StageErrorCodes.SlotNotFound);
                }
            }
        }

        if (Regulation.QualificationRules is { } qualification)
        {
            foreach (var path in qualification.Paths)
            {
                if (path.Destination.StageId.Equals(Id) && FindSlot(path.Destination.SlotKey) is null)
                {
                    throw new DomainException(
                        $"Slot '{path.Destination.SlotKey}' was not found.",
                        StageErrorCodes.SlotNotFound);
                }
            }
        }

        if (_slots.Count == 0)
        {
            return;
        }

        foreach (var assignment in _directAssignments
                     .Select(assignment => new
                     {
                         assignment,
                         slot = FindSlot(assignment.SlotKey) ??
                                throw new DomainException($"Slot '{assignment.SlotKey}' was not found.",
                                    StageErrorCodes.SlotNotFound)
                     })
                     .Where(t => t.slot.EntryId?.Equals(t.assignment.EntryId) != true)
                     .Select(t => t.assignment))
        {
            throw new DomainException(
                $"Direct assignment for slot '{assignment.SlotKey}' is out of sync with slot entry.",
                StageErrorCodes.InvalidConfiguration);
        }

        foreach (var fixture in EnumerateFixtures())
        {
            if (fixture.SlotAKey is not null && FindSlot(fixture.SlotAKey) is null)
            {
                throw new DomainException(
                    $"Slot '{fixture.SlotAKey}' was not found.",
                    StageErrorCodes.SlotNotFound);
            }

            if (fixture.SlotBKey is not null && FindSlot(fixture.SlotBKey) is null)
            {
                throw new DomainException(
                    $"Slot '{fixture.SlotBKey}' was not found.",
                    StageErrorCodes.SlotNotFound);
            }
        }

        foreach (var slot in _slots)
        {
            var localFeedCount = 0;
            if (_directAssignments.Any(a => string.Equals(a.SlotKey, slot.SlotKey, StringComparison.Ordinal)))
            {
                localFeedCount++;
            }

            if (IsSlotFedByLocalProgression(slot.SlotKey))
            {
                localFeedCount++;
            }

            if (IsSlotFedByLocalQualification(slot.SlotKey))
            {
                localFeedCount++;
            }

            if (localFeedCount > 1)
            {
                throw new DomainException(
                    $"Slot '{slot.SlotKey}' has multiple local feeds.",
                    StageErrorCodes.MultipleFeeds);
            }
        }
    }

    private void EnsureInitialRoundPlayableWhenPositional()
    {
        if (_rounds.Count == 0)
        {
            return;
        }

        var hasSlottedFixtures = EnumerateFixtures().Any(f => f.SlotAKey is not null || f.SlotBKey is not null);
        if (!hasSlottedFixtures)
        {
            return;
        }

        var initialRound = _rounds[0];
        var playable = initialRound.Fixtures.Any(IsFixturePlayable);
        if (!playable)
        {
            throw new DomainException(
                "Positional knockout requires at least one playable fixture in the initial round before Start.",
                StageErrorCodes.NotReady);
        }
    }

    private bool IsFixturePlayable(Fixture fixture)
    {
        if (fixture.SlotAKey is null || fixture.SlotBKey is null)
        {
            return false;
        }

        var slotA = FindSlot(fixture.SlotAKey);
        var slotB = FindSlot(fixture.SlotBKey);
        return slotA?.EntryId is not null && slotB?.EntryId is not null;
    }
}
