// -----------------------------------------------------------------------
// <copyright file="Group.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// A pool (poule) within a stage, holding ordered entry references.
/// </summary>
[DebuggerDisplay("{Name}")]
public sealed class Group : Entity<GroupId>
{
    /// <summary>
    /// Maximum allowed length of a group name after trim.
    /// </summary>
    public const int NameMaxLength = 100;

    private readonly List<EntryId> _entryIds = [];

    internal Group(GroupId id, string name)
        : base(id) =>
        Name = NormalizeName(name);

    /// <summary>
    /// Gets the group display name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the ordered entry identities assigned to this group.
    /// </summary>
    public IReadOnlyList<EntryId> EntryIds => _entryIds.AsReadOnly();

    internal void Rename(string name)
    {
        var normalized = NormalizeName(name);
        if (string.Equals(Name, normalized, StringComparison.Ordinal))
        {
            return;
        }

        Name = normalized;
    }

    internal bool Assign(EntryId entryId)
    {
        if (_entryIds.Contains(entryId))
        {
            return false;
        }

        _entryIds.Add(entryId);
        return true;
    }

    internal bool Remove(EntryId entryId) => _entryIds.Remove(entryId);

    internal bool Contains(EntryId entryId) => _entryIds.Contains(entryId);

    private static string NormalizeName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var trimmed = name.Trim();
        return trimmed.Length switch
        {
            0 => throw new DomainException("Group name cannot be empty.", StageErrorCodes.InvalidDisplayName),
            > NameMaxLength => throw new DomainException(
                $"Group name cannot exceed {NameMaxLength} characters.",
                StageErrorCodes.InvalidDisplayName),
            _ => trimmed
        };
    }
}
