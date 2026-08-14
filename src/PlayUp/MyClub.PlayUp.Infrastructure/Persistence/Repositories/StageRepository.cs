// -----------------------------------------------------------------------
// <copyright file="StageRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core repository for the Stage aggregate structure.
/// </summary>
internal sealed class StageRepository(PlayUpDbContext context) : IStageRepository
{
    /// <inheritdoc />
    public async Task<Stage?> GetByIdAsync(StageId id, CancellationToken cancellationToken = default)
    {
        var alreadyTracked = context.Set<Stage>().Local.Any(candidate => candidate.Id.Equals(id));

        var stage = await context.Set<Stage>()
            .Include(candidate => candidate.Groups)
            .Include(candidate => candidate.Rounds)
            .ThenInclude(round => round.Fixtures)
            .Include(candidate => candidate.Matchdays)
            .ThenInclude(matchday => matchday.Fixtures)
            .Include(candidate => candidate.Slots)
            .Include(candidate => candidate.DirectAssignments)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (stage is null)
        {
            return null;
        }

        if (!alreadyTracked)
        {
            await HydrateOrderedCollectionsAsync(stage, cancellationToken).ConfigureAwait(false);
        }

        return stage;
    }

    /// <inheritdoc />
    public void Add(Stage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);
        context.Set<Stage>().Add(stage);
    }

    private async Task HydrateOrderedCollectionsAsync(Stage stage, CancellationToken cancellationToken)
    {
        var groups = StageOrderedCollectionsAccessor.GetGroups(stage)
            .OrderBy(group => context.Entry(group).Property<int>("SortOrder").CurrentValue)
            .ToList();
        StageOrderedCollectionsAccessor.ReorderGroups(stage, groups);

        var groupIds = groups.Select(group => group.Id).ToArray();
        var entryRows = groupIds.Length == 0
            ? []
            : await context.Set<GroupEntryRef>()
                .Where(row => groupIds.AsEnumerable().Contains(row.GroupId))
                .OrderBy(row => row.SortOrder)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        foreach (var group in groups)
        {
            var orderedEntryIds = entryRows
                .Where(row => row.GroupId == group.Id)
                .OrderBy(row => row.SortOrder)
                .Select(row => row.EntryId);
            StageOrderedCollectionsAccessor.ReorderEntryIds(group, orderedEntryIds);
        }

        var rounds = StageOrderedCollectionsAccessor.GetRounds(stage)
            .OrderBy(round => context.Entry(round).Property<int>("SortOrder").CurrentValue)
            .ToList();
        StageOrderedCollectionsAccessor.ReorderRounds(stage, rounds);

        foreach (var round in rounds)
        {
            var fixtures = StageOrderedCollectionsAccessor.GetRoundFixtures(round)
                .OrderBy(fixture => context.Entry(fixture).Property<int>("SortOrder").CurrentValue)
                .ToList();
            StageOrderedCollectionsAccessor.ReorderRoundFixtures(round, fixtures);
            await HydrateAttachmentsAsync(fixtures, cancellationToken).ConfigureAwait(false);
        }

        var matchdays = StageOrderedCollectionsAccessor.GetMatchdays(stage)
            .OrderBy(matchday => context.Entry(matchday).Property<int>("SortOrder").CurrentValue)
            .ToList();
        StageOrderedCollectionsAccessor.ReorderMatchdays(stage, matchdays);

        foreach (var matchday in matchdays)
        {
            var fixtures = StageOrderedCollectionsAccessor.GetMatchdayFixtures(matchday)
                .OrderBy(fixture => context.Entry(fixture).Property<int>("SortOrder").CurrentValue)
                .ToList();
            StageOrderedCollectionsAccessor.ReorderMatchdayFixtures(matchday, fixtures);
            await HydrateAttachmentsAsync(fixtures, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task HydrateAttachmentsAsync(List<Fixture> fixtures, CancellationToken cancellationToken)
    {
        if (fixtures.Count == 0)
        {
            return;
        }

        var fixtureIds = fixtures.Select(fixture => fixture.Id).ToArray();
        var rows = await context.Set<FixtureAttachmentRef>()
            .Where(row => fixtureIds.AsEnumerable().Contains(row.FixtureId))
            .OrderBy(row => row.LegIndex)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var fixture in fixtures)
        {
            var attachments = rows
                .Where(row => row.FixtureId == fixture.Id)
                .OrderBy(row => row.LegIndex)
                .Select(row => new MatchAttachment(row.MatchId, row.LegIndex));
            StageOrderedCollectionsAccessor.ReplaceAttachments(fixture, attachments);
        }
    }
}
