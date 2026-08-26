// -----------------------------------------------------------------------
// <copyright file="Competition.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions.Events;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Domain.Competitions;

/// <summary>
/// Aggregate root for a competition: lifecycle, entries, ordered stage references, and regulation.
/// </summary>
[DebuggerDisplay("{Name} ({Status})")]
public sealed class Competition : AggregateRoot<CompetitionId>
{
    private readonly List<CompetitionEntry> _entries = [];
    private readonly List<StageId> _stageIds = [];

    private Competition(CompetitionId id, CompetitionName name, Regulation regulation)
        : base(id)
    {
        Name = name;
        Regulation = regulation;
        Status = CompetitionStatus.Draft;
    }

    /// <summary>
    /// Gets the competition name.
    /// </summary>
    public CompetitionName Name { get; private set; }

    /// <summary>
    /// Gets the optional abbreviated name.
    /// </summary>
    public ShortName? ShortName { get; private set; }

    /// <summary>
    /// Gets the optional Media reference for the competition logo.
    /// </summary>
    public LogoMediaId? LogoMediaId { get; private set; }

    /// <summary>
    /// Gets the optional declared competition start.
    /// </summary>
    public DateTimeOffset? ScheduledStart { get; private set; }

    /// <summary>
    /// Gets the optional declared competition end.
    /// </summary>
    public DateTimeOffset? ScheduledEnd { get; private set; }

    /// <summary>
    /// Gets the competition regulation (entry, match, and standing rules).
    /// </summary>
    public Regulation Regulation { get; private set; }

    /// <summary>
    /// Gets the competition lifecycle status.
    /// </summary>
    public CompetitionStatus Status { get; private set; }

    /// <summary>
    /// Gets how the competition was completed, if completed.
    /// </summary>
    public CompletionMode? CompletionMode { get; private set; }

    /// <summary>
    /// Gets the competition entries.
    /// </summary>
    public IReadOnlyList<CompetitionEntry> Entries => _entries.AsReadOnly();

    /// <summary>
    /// Gets the ordered stage identities referenced by this competition.
    /// </summary>
    public IReadOnlyList<StageId> StageIds => _stageIds.AsReadOnly();

    /// <summary>
    /// Creates a new competition in Draft status with a generated identity.
    /// </summary>
    /// <param name="name">The competition name.</param>
    /// <param name="regulation">The competition regulation.</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <returns>The created competition.</returns>
    public static Competition Create(CompetitionName name, Regulation regulation, IClock clock) =>
        Create(name, regulation, CompetitionId.New(), clock);

    /// <summary>
    /// Creates a new competition in Draft status with an explicit identity.
    /// </summary>
    /// <param name="name">The competition name.</param>
    /// <param name="regulation">The competition regulation.</param>
    /// <param name="id">The competition identity (must not be empty).</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <returns>The created competition.</returns>
    public static Competition Create(CompetitionName name, Regulation regulation, CompetitionId id, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(regulation);
        ArgumentNullException.ThrowIfNull(clock);

        var competition = new Competition(id, name, regulation.Copy());
        competition.Raise(new CompetitionCreated(competition.Id, name.Value, clock));
        return competition;
    }

    /// <summary>
    /// Replaces the competition regulation as a whole.
    /// Allowed in Draft or Ready; Ready is demoted to Draft.
    /// </summary>
    /// <param name="regulation">The new regulation.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void ReplaceRegulation(Regulation regulation, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(regulation);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();
        DemoteToDraftIfReady();

        Regulation = regulation.Copy();
        Raise(new CompetitionRegulationReplaced(Id, clock));
    }

    /// <summary>
    /// Gets an entry by identity.
    /// </summary>
    /// <param name="entryId">The entry identity.</param>
    /// <returns>The entry.</returns>
    public CompetitionEntry GetEntry(EntryId entryId) =>
        _entries.FirstOrDefault(e => e.Id.Equals(entryId))
        ?? throw new DomainException($"Entry '{entryId}' was not found.", CompetitionErrorCodes.EntryNotFound);

    /// <summary>
    /// Finds the occupying entry for a team, if any.
    /// </summary>
    /// <param name="teamId">The team identity.</param>
    /// <returns>The occupying entry, or <see langword="null"/>.</returns>
    public CompetitionEntry? FindEntry(TeamId teamId) =>
        _entries.FirstOrDefault(e => e.TeamId.Equals(teamId) && e.IsOccupying);

    /// <summary>
    /// Determines whether a team currently occupies a slot in this competition.
    /// </summary>
    /// <param name="teamId">The team identity.</param>
    /// <returns><see langword="true"/> if an occupying entry exists; otherwise, <see langword="false"/>.</returns>
    public bool ContainsTeam(TeamId teamId) => FindEntry(teamId) is not null;

    /// <summary>
    /// Determines whether a stage is referenced by this competition.
    /// </summary>
    /// <param name="stageId">The stage identity.</param>
    /// <returns><see langword="true"/> if the stage is referenced; otherwise, <see langword="false"/>.</returns>
    public bool HasStage(StageId stageId) => _stageIds.Contains(stageId);

    /// <summary>
    /// Renames the competition.
    /// </summary>
    /// <param name="name">The new name.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void Rename(CompetitionName name, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();

        Name = name;
        Raise(new CompetitionRenamed(Id, name.Value, clock));
    }

    /// <summary>
    /// Updates competition presentation metadata (short name and logo). Null clears a field.
    /// </summary>
    /// <param name="shortName">The short name, or <see langword="null"/> to clear.</param>
    /// <param name="logoMediaId">The logo Media reference, or <see langword="null"/> to clear.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void UpdatePresentation(ShortName? shortName, LogoMediaId? logoMediaId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();

        ShortName = shortName;
        LogoMediaId = logoMediaId;
        Raise(new CompetitionPresentationUpdated(Id, shortName?.Value, logoMediaId?.Value, clock));
    }

    /// <summary>
    /// Sets or clears declared competition schedule dates.
    /// When both are set, <paramref name="scheduledStart"/> must be less than or equal to <paramref name="scheduledEnd"/>.
    /// </summary>
    /// <param name="scheduledStart">Declared start, or <see langword="null"/>.</param>
    /// <param name="scheduledEnd">Declared end, or <see langword="null"/>.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void SetSchedule(DateTimeOffset? scheduledStart, DateTimeOffset? scheduledEnd, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();

        if (scheduledStart is { } start && scheduledEnd is { } end && start > end)
        {
            throw new DomainException(
                "Scheduled start cannot be after scheduled end.",
                CompetitionErrorCodes.InvalidSchedule);
        }

        ScheduledStart = scheduledStart;
        ScheduledEnd = scheduledEnd;
        Raise(new CompetitionScheduleSet(Id, scheduledStart, scheduledEnd, clock));
    }

    /// <summary>
    /// Adds a participating team.
    /// </summary>
    /// <param name="teamId">The team identity.</param>
    /// <param name="displayName">The display name for the entry.</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <param name="presentation">Optional presentation metadata.</param>
    /// <returns>The created entry.</returns>
    public CompetitionEntry AddEntry(
        TeamId teamId,
        string displayName,
        IClock clock,
        EntryPresentation? presentation = null) =>
        AddEntry(teamId, displayName, EntryId.New(), clock, presentation);

    /// <summary>
    /// Adds a team entry with an explicit entry identity.
    /// </summary>
    /// <param name="teamId">The team identity.</param>
    /// <param name="displayName">The display name for the entry.</param>
    /// <param name="entryId">The entry identity (must not be empty).</param>
    /// <param name="clock">The clock used for domain events.</param>
    /// <param name="presentation">Optional presentation metadata.</param>
    /// <returns>The created entry.</returns>
    public CompetitionEntry AddEntry(
        TeamId teamId,
        string displayName,
        EntryId entryId,
        IClock clock,
        EntryPresentation? presentation = null)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();
        DemoteToDraftIfReady();

        if (ContainsTeam(teamId))
        {
            throw new DomainException(
                $"Team '{teamId}' already has an occupying entry.",
                CompetitionErrorCodes.DuplicateTeam);
        }

        var entry = new CompetitionEntry(entryId, teamId, displayName);
        if (presentation is not null)
        {
            entry.UpdatePresentation(presentation);
        }

        _entries.Add(entry);
        Raise(new CompetitionEntryAdded(Id, entry.Id, teamId, clock));
        return entry;
    }

    /// <summary>
    /// Renames an entry display name.
    /// </summary>
    /// <param name="entryId">The entry identity.</param>
    /// <param name="displayName">The new display name.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void RenameEntry(EntryId entryId, string displayName, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();

        var entry = GetEntry(entryId);
        entry.Rename(displayName);
        Raise(new CompetitionEntryRenamed(Id, entryId, entry.DisplayName, clock));
    }

    /// <summary>
    /// Updates entry presentation metadata. Null fields clear the corresponding value.
    /// </summary>
    /// <param name="entryId">The entry identity.</param>
    /// <param name="presentation">The presentation to apply.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void UpdateEntryPresentation(EntryId entryId, EntryPresentation presentation, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();

        var entry = GetEntry(entryId);
        entry.UpdatePresentation(presentation);
        Raise(new CompetitionEntryPresentationUpdated(Id, entryId, clock));
    }

    /// <summary>
    /// Withdraws an entry from the competition.
    /// </summary>
    /// <param name="entryId">The entry identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void WithdrawEntry(EntryId entryId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureCanWithdrawOrExclude(allowAfterStart: true);

        var entry = GetEntry(entryId);
        entry.Withdraw();
        Raise(new CompetitionEntryWithdrawn(Id, entry.Id, entry.TeamId, clock));
    }

    /// <summary>
    /// Excludes an entry (organizer decision). Allowed only before the competition starts.
    /// </summary>
    /// <param name="entryId">The entry identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void ExcludeEntry(EntryId entryId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureCanWithdrawOrExclude(allowAfterStart: false);

        var entry = GetEntry(entryId);
        entry.Exclude();
        Raise(new CompetitionEntryExcluded(Id, entry.Id, entry.TeamId, clock));
    }

    /// <summary>
    /// Adds a stage reference to the competition.
    /// </summary>
    /// <param name="stageId">The stage identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void AddStage(StageId stageId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();
        DemoteToDraftIfReady();

        if (_stageIds.Contains(stageId))
        {
            throw new DomainException(
                $"Stage '{stageId}' is already attached.",
                CompetitionErrorCodes.DuplicateStage);
        }

        _stageIds.Add(stageId);
        Raise(new CompetitionStageAdded(Id, stageId, clock));
    }

    /// <summary>
    /// Removes a stage reference from the competition.
    /// </summary>
    /// <param name="stageId">The stage identity.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void RemoveStage(StageId stageId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();
        DemoteToDraftIfReady();

        if (!_stageIds.Remove(stageId))
        {
            throw new DomainException(
                $"Stage '{stageId}' was not found.",
                CompetitionErrorCodes.StageNotFound);
        }

        Raise(new CompetitionStageRemoved(Id, stageId, clock));
    }

    /// <summary>
    /// Sets the order of stage references. The list must be a permutation of the current stage ids.
    /// </summary>
    /// <param name="orderedStageIds">The new ordered stage identities.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void SetStageOrder(IReadOnlyList<StageId> orderedStageIds, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(orderedStageIds);
        ArgumentNullException.ThrowIfNull(clock);
        EnsureDraftOrReady();

        if (orderedStageIds.Count != _stageIds.Count
            || orderedStageIds.Distinct().Count() != orderedStageIds.Count
            || orderedStageIds.Any(id => !_stageIds.Contains(id)))
        {
            throw new DomainException(
                "Stage order must be a permutation of the current stage ids.",
                CompetitionErrorCodes.InvalidStageOrder);
        }

        if (_stageIds.SequenceEqual(orderedStageIds))
        {
            return;
        }

        DemoteToDraftIfReady();
        _stageIds.Clear();
        _stageIds.AddRange(orderedStageIds);
        Raise(new CompetitionStageOrderChanged(Id, [.._stageIds], clock));
    }

    /// <summary>
    /// Prepares the competition configuration (transitions Draft to Ready).
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    public void Prepare(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStatus(CompetitionStatus.Draft, "Competition can only be prepared from Draft.");

        if (_stageIds.Count == 0)
        {
            throw new DomainException(
                "Competition requires at least one stage before Prepare.",
                CompetitionErrorCodes.InvalidTransition);
        }

        if (_entries.All(e => e.Status != EntryStatus.Active))
        {
            throw new DomainException(
                "Competition requires at least one active entry before Prepare.",
                CompetitionErrorCodes.InvalidTransition);
        }

        Status = CompetitionStatus.Ready;
        Raise(new CompetitionPrepared(Id, clock));
    }

    /// <summary>
    /// Starts the competition (transitions Ready to Running).
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    public void Start(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStatus(CompetitionStatus.Ready, "Competition can only be started from Ready.");
        Status = CompetitionStatus.Running;
        Raise(new CompetitionStarted(Id, clock));
    }

    /// <summary>
    /// Suspends a running competition.
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    public void Suspend(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStatus(CompetitionStatus.Running, "Competition can only be suspended from Running.");
        Status = CompetitionStatus.Suspended;
        Raise(new CompetitionSuspended(Id, clock));
    }

    /// <summary>
    /// Resumes a suspended competition.
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    public void Resume(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStatus(CompetitionStatus.Suspended, "Competition can only be resumed from Suspended.");
        Status = CompetitionStatus.Running;
        Raise(new CompetitionResumed(Id, clock));
    }

    /// <summary>
    /// Completes the competition.
    /// </summary>
    /// <param name="mode">How the competition is completed.</param>
    /// <param name="clock">The clock used for domain events.</param>
    public void Complete(CompletionMode mode, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (Status is not (CompetitionStatus.Running or CompetitionStatus.Suspended))
        {
            throw new DomainException(
                $"Competition cannot be completed from '{Status}'.",
                CompetitionErrorCodes.InvalidTransition);
        }

        if (!Enum.IsDefined(mode))
        {
            throw new DomainException($"Unknown completion mode '{mode}'.", CompetitionErrorCodes.InvalidTransition);
        }

        Status = CompetitionStatus.Completed;
        CompletionMode = mode;
        Raise(new CompetitionCompleted(Id, mode, clock));
    }

    /// <summary>
    /// Archives a completed competition.
    /// </summary>
    /// <param name="clock">The clock used for domain events.</param>
    public void Archive(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        EnsureStatus(CompetitionStatus.Completed, "Competition can only be archived from Completed.");
        Status = CompetitionStatus.Archived;
        Raise(new CompetitionArchived(Id, clock));
    }

    private void DemoteToDraftIfReady()
    {
        if (Status == CompetitionStatus.Ready)
        {
            Status = CompetitionStatus.Draft;
        }
    }

    private void EnsureDraftOrReady()
    {
        if (Status is not (CompetitionStatus.Draft or CompetitionStatus.Ready))
        {
            throw new DomainException(
                $"Operation is not allowed when status is '{Status}'.",
                CompetitionErrorCodes.InvalidTransition);
        }
    }

    private void EnsureCanWithdrawOrExclude(bool allowAfterStart)
    {
        if (Status is CompetitionStatus.Draft or CompetitionStatus.Ready)
        {
            return;
        }

        if (allowAfterStart && Status is CompetitionStatus.Running or CompetitionStatus.Suspended)
        {
            return;
        }

        throw new DomainException(
            $"Operation is not allowed when status is '{Status}'.",
            CompetitionErrorCodes.InvalidTransition);
    }

    private void EnsureStatus(CompetitionStatus expected, string message)
    {
        if (Status != expected)
        {
            throw new DomainException(message, CompetitionErrorCodes.InvalidTransition);
        }
    }
}
