// -----------------------------------------------------------------------
// <copyright file="ProgressionRulesPathDiff.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// P-4 path diff between persisted <see cref="ProgressionRules"/> snapshots (before / after).
/// </summary>
public static class ProgressionRulesPathDiff
{
    /// <summary>
    /// Compares progression path sets. <paramref name="afterRules"/> null means clear (all before paths removed).
    /// </summary>
    public static ProgressionRulesPathDiffResult Compare(
        ProgressionRules? beforeRules,
        ProgressionRules? afterRules)
    {
        var beforeMap = Index(beforeRules);
        var afterMap = Index(afterRules);

        var added = new List<ProgressionPath>();
        var removed = new List<ProgressionPath>();
        var replacedBefore = new List<ProgressionPath>();
        var unchanged = new List<ProgressionPath>();

        foreach (var (key, beforePath) in beforeMap)
        {
            if (!afterMap.TryGetValue(key, out var afterPath))
            {
                removed.Add(beforePath);
            }
            else if (!beforePath.Equals(afterPath))
            {
                replacedBefore.Add(beforePath);
            }
            else
            {
                unchanged.Add(beforePath);
            }
        }

        foreach (var (key, afterPath) in afterMap)
        {
            if (!beforeMap.ContainsKey(key))
            {
                added.Add(afterPath);
            }
        }

        return new ProgressionRulesPathDiffResult(added, removed, replacedBefore, unchanged);
    }

    private static Dictionary<ProgressionPathSourceKey, ProgressionPath> Index(ProgressionRules? rules)
    {
        if (rules is null)
        {
            return [];
        }

        var map = new Dictionary<ProgressionPathSourceKey, ProgressionPath>();
        foreach (var path in rules.Paths)
        {
            map[ProgressionPathSourceKey.FromPath(path)] = path;
        }

        return map;
    }
}

/// <summary>
/// Result of <see cref="ProgressionRulesPathDiff.Compare"/>.
/// </summary>
public sealed class ProgressionRulesPathDiffResult
{
    internal ProgressionRulesPathDiffResult(
        IReadOnlyList<ProgressionPath> added,
        IReadOnlyList<ProgressionPath> removed,
        IReadOnlyList<ProgressionPath> replacedBefore,
        IReadOnlyList<ProgressionPath> unchanged)
    {
        Added = added;
        Removed = removed;
        ReplacedBefore = replacedBefore;
        Unchanged = unchanged;
        Touched = [.. removed, .. replacedBefore];
    }

    /// <summary>Gets paths whose source key exists only in after.</summary>
    public IReadOnlyList<ProgressionPath> Added { get; }

    /// <summary>Gets paths present in before whose source key is absent in after.</summary>
    public IReadOnlyList<ProgressionPath> Removed { get; }

    /// <summary>Gets the before path when destination changed for the same source key.</summary>
    public IReadOnlyList<ProgressionPath> ReplacedBefore { get; }

    /// <summary>Gets paths equal in both snapshots.</summary>
    public IReadOnlyList<ProgressionPath> Unchanged { get; }

    /// <summary>Gets removed ∪ replaced before snapshots (WC-1 scope).</summary>
    public IReadOnlyList<ProgressionPath> Touched { get; }

    /// <summary>Gets a value indicating whether any path was removed or replaced.</summary>
    public bool HasStructuralPathChange => Touched.Count > 0;
}
