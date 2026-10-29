// -----------------------------------------------------------------------
// <copyright file="SqlCommandCounterInterceptor.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MyClub.PlayUp.Development.Tests.Diagnostics;

/// <summary>
/// Counts SQL commands executed by EF Core (read baseline / regression guard).
/// </summary>
internal sealed class SqlCommandCounterInterceptor : DbCommandInterceptor
{
    private int _commandCount;
    private readonly List<string> _commandTexts = [];

    public int CommandCount => Volatile.Read(ref _commandCount);

    public IReadOnlyList<string> CommandTexts
    {
        get
        {
            lock (_commandTexts)
            {
                return [.. _commandTexts];
            }
        }
    }

    public void Reset()
    {
        Interlocked.Exchange(ref _commandCount, 0);
        lock (_commandTexts)
        {
            _commandTexts.Clear();
        }
    }

    private void Record(DbCommand command)
    {
        Interlocked.Increment(ref _commandCount);
        lock (_commandTexts)
        {
            _commandTexts.Add(command.CommandText);
        }
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        Record(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Record(command);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Record(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Record(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        Record(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        Record(command);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }
}
