// -----------------------------------------------------------------------
// <copyright file="CompetitionAdvisoryLockTimingInterceptor.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MyClub.PlayUp.Development.Tests.Diagnostics;

/// <summary>
/// Measures competition advisory lock wait (acquisition command) and retention (until async commit/rollback).
/// </summary>
internal sealed class CompetitionAdvisoryLockTimingInterceptor : DbCommandInterceptor
{
    private readonly CompetitionAdvisoryLockTimingState _state = new();

    public CompetitionAdvisoryLockTimingSnapshot? LastSnapshot => _state.LastSnapshot;

    public void Reset() => _state.Reset();

    internal void CompleteRetention() => _state.CompleteRetention();

    internal CompetitionAdvisoryLockTimingSnapshot RequireLastSnapshot() => _state.RequireSnapshot();

    private static bool IsAdvisoryLockCommand(DbCommand command) =>
        command.CommandText.Contains("pg_advisory_xact_lock", StringComparison.Ordinal);

    private void OnAdvisoryLockExecuting(DbCommand command)
    {
        if (!IsAdvisoryLockCommand(command))
        {
            return;
        }

        _state.BeginWait();
    }

    private void OnAdvisoryLockExecuted() => _state.EndWait();

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        OnAdvisoryLockExecuting(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        OnAdvisoryLockExecuting(command);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        if (IsAdvisoryLockCommand(command))
        {
            OnAdvisoryLockExecuted();
        }

        return base.NonQueryExecuted(command, eventData, result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (IsAdvisoryLockCommand(command))
        {
            OnAdvisoryLockExecuted();
        }

        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        OnAdvisoryLockExecuting(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        OnAdvisoryLockExecuting(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        if (IsAdvisoryLockCommand(command))
        {
            OnAdvisoryLockExecuted();
        }

        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        if (IsAdvisoryLockCommand(command))
        {
            OnAdvisoryLockExecuted();
        }

        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        OnAdvisoryLockExecuting(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        OnAdvisoryLockExecuting(command);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        if (IsAdvisoryLockCommand(command))
        {
            OnAdvisoryLockExecuted();
        }

        return base.ScalarExecuted(command, eventData, result);
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        if (IsAdvisoryLockCommand(command))
        {
            OnAdvisoryLockExecuted();
        }

        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }
}

internal sealed class CompetitionAdvisoryLockTransactionInterceptor(
    CompetitionAdvisoryLockTimingInterceptor timing) : DbTransactionInterceptor
{
    public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
    {
        timing.CompleteRetention();
        base.TransactionCommitted(transaction, eventData);
    }

    public override Task TransactionCommittedAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        timing.CompleteRetention();
        return base.TransactionCommittedAsync(transaction, eventData, cancellationToken);
    }

    public override void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData)
    {
        timing.CompleteRetention();
        base.TransactionRolledBack(transaction, eventData);
    }

    public override Task TransactionRolledBackAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        timing.CompleteRetention();
        return base.TransactionRolledBackAsync(transaction, eventData, cancellationToken);
    }
}

internal sealed class CompetitionAdvisoryLockTimingState
{
    private static readonly AsyncLocal<TimingFrame?> Current = new();

    private readonly object _sync = new();

    public CompetitionAdvisoryLockTimingSnapshot? LastSnapshot =>
        Current.Value?.Snapshot;

    public CompetitionAdvisoryLockTimingSnapshot RequireSnapshot()
    {
        var snapshot = Current.Value?.Snapshot;
        return snapshot ?? throw new InvalidOperationException(
            "Competition lock timing snapshot is missing (transaction commit/rollback was not observed).");
    }

    public void Reset()
    {
        Current.Value = new TimingFrame();
    }

    public void BeginWait()
    {
        var frame = EnsureFrame();
        lock (_sync)
        {
            frame.WaitStopwatch = Stopwatch.StartNew();
            frame.AcquisitionCompleted = false;
            frame.RetentionStopwatch = null;
            frame.LockWait = TimeSpan.Zero;
            frame.LockRetention = TimeSpan.Zero;
            frame.Snapshot = null;
        }
    }

    public void EndWait()
    {
        var frame = EnsureFrame();
        lock (_sync)
        {
            if (frame.WaitStopwatch is null)
            {
                return;
            }

            frame.WaitStopwatch.Stop();
            frame.LockWait = frame.WaitStopwatch.Elapsed;
            frame.WaitStopwatch = null;
            frame.RetentionStopwatch = Stopwatch.StartNew();
            frame.AcquisitionCompleted = true;
        }
    }

    public void CompleteRetention()
    {
        var frame = Current.Value;
        if (frame is null)
        {
            return;
        }

        lock (_sync)
        {
            if (!frame.AcquisitionCompleted || frame.RetentionStopwatch is null)
            {
                return;
            }

            frame.RetentionStopwatch.Stop();
            frame.LockRetention = frame.RetentionStopwatch.Elapsed;
            frame.RetentionStopwatch = null;
            frame.Snapshot = new CompetitionAdvisoryLockTimingSnapshot(frame.LockWait, frame.LockRetention);
        }
    }

    private static TimingFrame EnsureFrame()
    {
        if (Current.Value is null)
        {
            Current.Value = new TimingFrame();
        }

        return Current.Value;
    }

    private sealed class TimingFrame
    {
        public Stopwatch? WaitStopwatch;
        public Stopwatch? RetentionStopwatch;
        public TimeSpan LockWait;
        public TimeSpan LockRetention;
        public bool AcquisitionCompleted;
        public CompetitionAdvisoryLockTimingSnapshot? Snapshot;
    }
}

internal readonly record struct CompetitionAdvisoryLockTimingSnapshot(
    TimeSpan LockWait,
    TimeSpan LockRetention);
