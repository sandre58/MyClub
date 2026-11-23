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
using MyClub.PlayUp.Infrastructure.DependencyInjection;

namespace MyClub.PlayUp.Infrastructure.Persistence;

/// <summary>
/// Play'Up EF Core unit of work for Play'Up aggregate persistence.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PlayUpDbContext"/> class.
/// </remarks>
/// <param name="options">The options for this context.</param>
/// <param name="lockOptions">Competition advisory lock timeout settings.</param>
public sealed class PlayUpDbContext(
    DbContextOptions<PlayUpDbContext> options,
    PlayUpCompetitionLockOptions lockOptions) : DbContext(options), IUnitOfWork
{
    private static ApplicationFailureException CreateCompetitionLockTimeoutException(Exception innerException) =>
        new(
            "Another operation is in progress on this competition. Please try again shortly.",
            ApplicationErrorCodes.CompetitionLockTimeout,
            innerException);

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
            var lockTimeoutLiteral = lockOptions.ToPostgresLockTimeoutLiteral();
            await Database.ExecuteSqlRawAsync(
                "SET LOCAL lock_timeout = {0}",
                [lockTimeoutLiteral],
                cancellationToken).ConfigureAwait(false);

            var key = CompetitionAdvisoryLock.Key(competitionId);
            try
            {
                await Database.ExecuteSqlAsync(
                    $"SELECT pg_advisory_xact_lock({key})",
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (CompetitionLockFailure.IsCompetitionLockContention(exception))
            {
                throw CreateCompetitionLockTimeoutException(exception);
            }

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
            catch (Exception exception) when (CompetitionLockFailure.IsCompetitionLockContention(exception))
            {
                throw CreateCompetitionLockTimeoutException(exception);
            }
        }
        finally
        {
            await transaction.DisposeAsync().ConfigureAwait(false);
        }
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
