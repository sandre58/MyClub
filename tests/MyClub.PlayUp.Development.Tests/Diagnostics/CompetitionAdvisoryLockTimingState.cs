// -----------------------------------------------------------------------
// <copyright file="CompetitionAdvisoryLockTimingState.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;

namespace MyClub.PlayUp.Development.Tests.Diagnostics;

internal static class CompetitionAdvisoryLockTimingState
{
    private static readonly AsyncLocal<TimingFrame?> Current = new();
    private static readonly object Sync = new();

    public static CompetitionAdvisoryLockTimingSnapshot? LastSnapshot =>
        Current.Value?.Snapshot;

    public static CompetitionAdvisoryLockTimingSnapshot RequireSnapshot()
    {
        var snapshot = Current.Value?.Snapshot;
        return snapshot ?? throw new InvalidOperationException(
            "Competition lock timing snapshot is missing (transaction commit/rollback was not observed).");
    }

    public static void Reset() => Current.Value = new TimingFrame();

    public static void BeginWait()
    {
        var frame = EnsureFrame();
        lock (Sync)
        {
            frame.WaitStopwatch = Stopwatch.StartNew();
            frame.AcquisitionCompleted = false;
            frame.RetentionStopwatch = null;
            frame.LockWait = TimeSpan.Zero;
            frame.LockRetention = TimeSpan.Zero;
            frame.Snapshot = null;
        }
    }

    public static void EndWait()
    {
        var frame = EnsureFrame();
        lock (Sync)
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

    public static void CompleteRetention()
    {
        var frame = Current.Value;
        if (frame is null)
        {
            return;
        }

        lock (Sync)
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
        public Stopwatch? WaitStopwatch { get; set; }
        public Stopwatch? RetentionStopwatch { get; set; }
        public TimeSpan LockWait { get; set; }
        public TimeSpan LockRetention { get; set; }
        public bool AcquisitionCompleted { get; set; }
        public CompetitionAdvisoryLockTimingSnapshot? Snapshot { get; set; }
    }
}

internal readonly record struct CompetitionAdvisoryLockTimingSnapshot(
    TimeSpan LockWait,
    TimeSpan LockRetention);
