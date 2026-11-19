// -----------------------------------------------------------------------
// <copyright file="PlayUpDbContext.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Infrastructure.Persistence;

/// <summary>
/// Play'Up EF Core unit of work for Play'Up aggregate persistence.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PlayUpDbContext"/> class.
/// </remarks>
/// <param name="options">The options for this context.</param>
public sealed class PlayUpDbContext(DbContextOptions<PlayUpDbContext> options) : DbContext(options), IUnitOfWork
{
    /// <inheritdoc />
    Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);

    /// <inheritdoc />
    void IUnitOfWork.ClearTrackedChanges() => ChangeTracker.Clear();

    /// <inheritdoc />
    async Task IUnitOfWork.ExecuteExclusiveAsync(
        CompetitionId competitionId,
        Func<CancellationToken, Task> write,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(write);
        EnsureExclusiveWriteStartsUntracked();
        if (!Database.IsRelational())
        {
            await write(cancellationToken).ConfigureAwait(false);
            return;
        }

        var transaction = await Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var key = AdvisoryKey(competitionId);
            await Database.ExecuteSqlAsync(
                $"SELECT pg_advisory_xact_lock({key})",
                cancellationToken).ConfigureAwait(false);
            try
            {
                await write(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (GroupEntriesUniqueViolation.IsMatch(exception))
            {
                throw new ApplicationFailureException(
                    "A group membership write conflicted with another writer on the same seat.",
                    ApplicationErrorCodes.GroupMembershipConflict);
            }
        }
        finally
        {
            await transaction.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static long AdvisoryKey(CompetitionId competitionId)
    {
        var bytes = competitionId.Value.ToByteArray();
        return BitConverter.ToInt64(bytes, 0) ^ BitConverter.ToInt64(bytes, 8);
    }

    /// <summary>
    /// A stage or match loaded before the lock is the instance the repositories return.
    /// The write must load those aggregates after the lock is held.
    /// </summary>
    private void EnsureExclusiveWriteStartsUntracked()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is Stage or Match)
            {
                throw new InvalidOperationException(
                    "An exclusive competition write must load its stage and matches inside the lock. A tracked stage or match is already present.");
            }
        }
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlayUpDbContext).Assembly);
}
