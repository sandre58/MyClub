// -----------------------------------------------------------------------
// <copyright file="CompetitionAdvisoryLockTimingInterceptor.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MyClub.PlayUp.Development.Tests.Diagnostics;

/// <summary>
/// Measures competition advisory lock wait (acquisition command) and retention (until async commit/rollback).
/// </summary>
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Registered as EF Core interceptor singleton; instance entry points required.")]
internal sealed class CompetitionAdvisoryLockTimingInterceptor : DbCommandInterceptor
{
    public CompetitionAdvisoryLockTimingSnapshot? LastSnapshot => CompetitionAdvisoryLockTimingState.LastSnapshot;

    public void Reset() => CompetitionAdvisoryLockTimingState.Reset();

    internal void CompleteRetention() => CompetitionAdvisoryLockTimingState.CompleteRetention();

    internal CompetitionAdvisoryLockTimingSnapshot RequireLastSnapshot() =>
        CompetitionAdvisoryLockTimingState.RequireSnapshot();

    private static bool IsAdvisoryLockCommand(DbCommand command) =>
        command.CommandText.Contains("pg_advisory_xact_lock", StringComparison.Ordinal);

    private void OnAdvisoryLockExecuting(DbCommand command)
    {
        if (!IsAdvisoryLockCommand(command))
        {
            return;
        }

        CompetitionAdvisoryLockTimingState.BeginWait();
    }

    private void OnAdvisoryLockExecuted() => CompetitionAdvisoryLockTimingState.EndWait();

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
