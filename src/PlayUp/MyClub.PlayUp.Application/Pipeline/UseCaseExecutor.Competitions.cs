// -----------------------------------------------------------------------
// <copyright file="UseCaseExecutor.Competitions.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Application.Pipeline;

/// <content>
/// Competition lifecycle, entries, presentation, and regulation commands.
/// </content>
public sealed partial class UseCaseExecutor
{
    /// <summary>
    /// Prepares a competition (Draft → Ready). Domain owns preconditions.
    /// </summary>
    /// <param name="competitionId">Competition identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the competition is prepared and persisted.</returns>
    public async Task PrepareCompetitionAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        EnsureCompetitionAllowsLifecycleMutation(competition);
        PrepareCompetition.Execute(competition, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogCompetitionPrepared(logger, competitionId.Value);
    }

    /// <summary>
    /// Starts a competition (Ready → Running). Domain owns preconditions.
    /// </summary>
    /// <param name="competitionId">Competition identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the competition is started and persisted.</returns>
    public async Task StartCompetitionAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        EnsureCompetitionAllowsLifecycleMutation(competition);
        StartCompetition.Execute(competition, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogCompetitionStarted(logger, competitionId.Value);
    }

    /// <summary>
    /// Completes a competition (Normal gated by <see cref="CompletionAnalyzer"/>).
    /// </summary>
    /// <param name="competitionId">Competition identity.</param>
    /// <param name="mode">Completion manner.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the competition is completed and persisted.</returns>
    public async Task CompleteCompetitionAsync(
        CompetitionId competitionId,
        CompletionMode mode,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var competitionStages =
            await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken).ConfigureAwait(false);
        var matchesByStage = await LoadMatchesByStageReadOnlyAsync(competitionStages, cancellationToken)
            .ConfigureAwait(false);
        var analysis = CompletionAnalyzer.Analyze(competition, competitionStages, matchesByStage);
        CompleteCompetition.Execute(competition, mode, analysis, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogCompetitionCompleted(logger, competitionId.Value, mode);
    }

    /// <summary>
    /// Archives a completed competition.
    /// </summary>
    /// <param name="competitionId">Competition identity.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the competition is archived and persisted.</returns>
    public async Task ArchiveCompetitionAsync(
        CompetitionId competitionId,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        ArchiveCompetition.Execute(competition, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogCompetitionArchived(logger, competitionId.Value);
    }

    /// <summary>
    /// Creates a Competition (bootstrap regulation), persists it, and returns <see cref="WorkspaceSummaryDto"/>.
    /// </summary>
    /// <param name="name">Display name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Workspace summary for the new Draft competition.</returns>
    public async Task<WorkspaceSummaryDto> CreateCompetitionAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var competition = CreateCompetition.Execute(name, clock);
        competitions.Add(competition);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogCompetitionCreated(logger, competition.Id.Value);
        return WorkspaceSummaryAssembler.Assemble(competition);
    }

    /// <summary>
    /// Adds an entry and returns the updated <see cref="StructureViewDto"/>.
    /// </summary>
    public async Task<StructureViewDto> AddEntryAsync(
        CompetitionId competitionId,
        string displayName,
        Guid? teamId = null,
        string? shortName = null,
        Guid? logoMediaId = null,
        string? primaryColor = null,
        string? secondaryColor = null,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        await EnsureLogoMediaExistsAsync(logoMediaId, cancellationToken).ConfigureAwait(false);
        var resolvedShortName = string.IsNullOrWhiteSpace(shortName)
            ? ShortName.FromDisplayName(displayName)
            : ShortName.CreateRequired(shortName);
        var presentation = new EntryPresentation(
            resolvedShortName,
            LogoMediaId.Create(logoMediaId),
            TeamColor.Create(primaryColor),
            TeamColor.Create(secondaryColor));
        AddEntry.Execute(
            competition,
            displayName,
            clock,
            teamId is null ? null : new TeamId(teamId.Value),
            presentation);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates competition presentation and returns the updated structure view.
    /// </summary>
    public async Task<StructureViewDto> UpdateCompetitionPresentationAsync(
        CompetitionId competitionId,
        string? shortName,
        Guid? logoMediaId,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        await EnsureLogoMediaExistsAsync(logoMediaId, cancellationToken).ConfigureAwait(false);
        UpdateCompetitionPresentation.Execute(competition, shortName, logoMediaId, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets declared competition schedule and returns the updated structure view.
    /// </summary>
    public async Task<StructureViewDto> SetCompetitionScheduleAsync(
        CompetitionId competitionId,
        DateTimeOffset? scheduledStart,
        DateTimeOffset? scheduledEnd,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        SetCompetitionSchedule.Execute(competition, scheduledStart, scheduledEnd, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates entry presentation and returns the updated structure view.
    /// </summary>
    public async Task<StructureViewDto> UpdateEntryPresentationAsync(
        CompetitionId competitionId,
        EntryId entryId,
        string? shortName,
        Guid? logoMediaId,
        string? primaryColor,
        string? secondaryColor,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        await EnsureLogoMediaExistsAsync(logoMediaId, cancellationToken).ConfigureAwait(false);
        UpdateEntryPresentation.Execute(
            competition,
            entryId,
            shortName ?? string.Empty,
            logoMediaId,
            primaryColor,
            secondaryColor,
            clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Renames an entry and returns the updated structure view.
    /// </summary>
    public async Task<StructureViewDto> RenameEntryAsync(
        CompetitionId competitionId,
        EntryId entryId,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        RenameEntry.Execute(competition, entryId, displayName, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Withdraws an entry (forfait) and returns the updated structure view.
    /// </summary>
    public async Task<StructureViewDto> WithdrawEntryAsync(
        CompetitionId competitionId,
        EntryId entryId,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var (competitionStages, competitionMatches) =
            await LoadCompetitionStagesAndMatchesForUpdateAsync(competition, cancellationToken)
                .ConfigureAwait(false);
        WithdrawEntry.Execute(competition, entryId, competitionStages, competitionMatches, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Hard-deletes an entry and returns the updated structure view.
    /// </summary>
    public async Task<StructureViewDto> DeleteEntryAsync(
        CompetitionId competitionId,
        EntryId entryId,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var (competitionStages, competitionMatches) =
            await LoadCompetitionStagesAndMatchesForUpdateAsync(competition, cancellationToken)
                .ConfigureAwait(false);
        DeleteEntry.Execute(competition, entryId, competitionStages, competitionMatches, matches, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Hard-deletes several entries atomically and returns the updated structure view.
    /// </summary>
    public async Task<StructureViewDto> DeleteEntriesAsync(
        CompetitionId competitionId,
        IReadOnlyList<EntryId> entryIds,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var (competitionStages, competitionMatches) =
            await LoadCompetitionStagesAndMatchesForUpdateAsync(competition, cancellationToken)
                .ConfigureAwait(false);
        DeleteEntries.Execute(competition, entryIds, competitionStages, competitionMatches, matches, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Withdraws several entries atomically and returns the updated structure view.
    /// </summary>
    public async Task<StructureViewDto> WithdrawEntriesAsync(
        CompetitionId competitionId,
        IReadOnlyList<EntryId> entryIds,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var (competitionStages, competitionMatches) =
            await LoadCompetitionStagesAndMatchesForUpdateAsync(competition, cancellationToken)
                .ConfigureAwait(false);
        WithdrawEntries.Execute(competition, entryIds, competitionStages, competitionMatches, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes several declared members atomically and returns the updated structure view.
    /// </summary>
    public async Task<StructureViewDto> RemoveDeclaredMembersAsync(
        CompetitionId competitionId,
        EntryId entryId,
        IReadOnlyList<MemberId> memberIds,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var competitionMatches =
            await LoadCompetitionMatchesAsync(competition, cancellationToken).ConfigureAwait(false);
        RemoveDeclaredMembers.Execute(competition, entryId, memberIds, competitionMatches, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds a declared member to an entry roster and returns the updated structure view.
    /// </summary>
    public async Task<StructureViewDto> AddDeclaredMemberAsync(
        CompetitionId competitionId,
        EntryId entryId,
        string displayName,
        DeclaredMemberRole role,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        AddDeclaredMember.Execute(competition, entryId, displayName, role, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes a declared member from an entry roster and returns the updated structure view.
    /// </summary>
    public async Task<StructureViewDto> RemoveDeclaredMemberAsync(
        CompetitionId competitionId,
        EntryId entryId,
        MemberId memberId,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var competitionMatches =
            await LoadCompetitionMatchesAsync(competition, cancellationToken).ConfigureAwait(false);
        RemoveDeclaredMember.Execute(competition, entryId, memberId, competitionMatches, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Renames a declared member and returns the updated structure view.
    /// </summary>
    public async Task<StructureViewDto> RenameDeclaredMemberAsync(
        CompetitionId competitionId,
        EntryId entryId,
        MemberId memberId,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        RenameDeclaredMember.Execute(competition, entryId, memberId, displayName, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces competition regulation and returns the updated structure view.
    /// </summary>
    /// <param name="competitionId">Competition identity.</param>
    /// <param name="buildReplacement">
    /// Builds the replacement regulation from the current persisted regulation
    /// (so omitted disciplinary AllowedTypes can preserve existing rules).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<StructureViewDto> ReplaceRegulationAsync(
        CompetitionId competitionId,
        Func<Regulation, Regulation> buildReplacement,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(buildReplacement);

        var competition = await RequireCompetitionAsync(competitionId, cancellationToken).ConfigureAwait(false);
        var competitionStagesForUpdate = await LoadCompetitionStagesForUpdateAsync(competition, cancellationToken).ConfigureAwait(false);
        var regulation = buildReplacement(competition.Regulation);
        ReplaceRegulation.Execute(competition, competitionStagesForUpdate, regulation, clock);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await AssembleStructureViewAsync(competition, cancellationToken).ConfigureAwait(false);
    }
}
