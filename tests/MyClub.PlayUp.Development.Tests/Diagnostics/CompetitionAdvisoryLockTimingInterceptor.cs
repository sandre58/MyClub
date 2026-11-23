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
/// Measures competition advisory lock wait (acquisition command) and retention (until transaction end).
/// </summary>
internal sealed class CompetitionAdvisoryLockTimingInterceptor : DbCommandInterceptor
{
    private readonly CompetitionAdvisoryLockTimingState _state = new();

    public CompetitionAdvisoryLockTimingSnapshot? LastSnapshot => _state.LastSnapshot;

    public void Reset() => _state.Reset();

    internal void CompleteRetention() => _state.CompleteRetention();

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

    public override void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData)
    {
        timing.CompleteRetention();
        base.TransactionRolledBack(transaction, eventData);
    }
}

internal sealed class CompetitionAdvisoryLockTimingState
{
    private readonly object _sync = new();
    private Stopwatch? _waitStopwatch;
    private Stopwatch? _retentionStopwatch;
    private TimeSpan _lockWait;
    private TimeSpan _lockRetention;
    private bool _lockAcquisitionCompleted;

    public CompetitionAdvisoryLockTimingSnapshot? LastSnapshot { get; private set; }

    public void Reset()
    {
        lock (_sync)
        {
            _waitStopwatch = null;
            _retentionStopwatch = null;
            _lockWait = TimeSpan.Zero;
            _lockRetention = TimeSpan.Zero;
            _lockAcquisitionCompleted = false;
            LastSnapshot = null;
        }
    }

    public void BeginWait()
    {
        lock (_sync)
        {
            _waitStopwatch = Stopwatch.StartNew();
            _lockAcquisitionCompleted = false;
            _retentionStopwatch = null;
            _lockWait = TimeSpan.Zero;
            _lockRetention = TimeSpan.Zero;
        }
    }

    public void EndWait()
    {
        lock (_sync)
        {
            if (_waitStopwatch is null)
            {
                return;
            }

            _waitStopwatch.Stop();
            _lockWait = _waitStopwatch.Elapsed;
            _waitStopwatch = null;
            _retentionStopwatch = Stopwatch.StartNew();
            _lockAcquisitionCompleted = true;
        }
    }

    public void CompleteRetention()
    {
        lock (_sync)
        {
            if (!_lockAcquisitionCompleted || _retentionStopwatch is null)
            {
                return;
            }

            _retentionStopwatch.Stop();
            _lockRetention = _retentionStopwatch.Elapsed;
            _retentionStopwatch = null;
            LastSnapshot = new CompetitionAdvisoryLockTimingSnapshot(_lockWait, _lockRetention);
        }
    }
}

internal readonly record struct CompetitionAdvisoryLockTimingSnapshot(
    TimeSpan LockWait,
    TimeSpan LockRetention);
