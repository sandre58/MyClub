// -----------------------------------------------------------------------
// <copyright file="CompetitionEntry.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Competitions;

/// <summary>
/// Participation of a team in a competition.
/// </summary>
[DebuggerDisplay("{DisplayName} ({Status})")]
public sealed class CompetitionEntry : Entity<EntryId>
{
    /// <summary>
    /// Maximum allowed length of an entry display name after trim.
    /// </summary>
    public const int DisplayNameMaxLength = 100;

    internal CompetitionEntry(EntryId id, TeamId teamId, string displayName)
        : base(id)
    {
        TeamId = teamId;
        DisplayName = NormalizeDisplayName(displayName);
        Status = EntryStatus.Active;
    }

    /// <summary>
    /// Gets the referenced team.
    /// </summary>
    public TeamId TeamId { get; }

    /// <summary>
    /// Gets the display name shown for this participation.
    /// </summary>
    public string DisplayName { get; private set; }

    /// <summary>
    /// Gets the participation status.
    /// </summary>
    public EntryStatus Status { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this entry currently occupies the team slot.
    /// </summary>
    public bool IsOccupying => Status is EntryStatus.Active or EntryStatus.Qualified or EntryStatus.Eliminated;

    internal void Rename(string displayName) => DisplayName = NormalizeDisplayName(displayName);

    internal void Withdraw()
    {
        EnsureActive();
        Status = EntryStatus.Withdrawn;
    }

    internal void Exclude()
    {
        EnsureActive();
        Status = EntryStatus.Excluded;
    }

    private static string NormalizeDisplayName(string displayName)
    {
        ArgumentNullException.ThrowIfNull(displayName);
        var trimmed = displayName.Trim();
        return trimmed.Length switch
        {
            0 => throw new DomainException("Entry display name cannot be empty.",
                CompetitionErrorCodes.InvalidDisplayName),
            > DisplayNameMaxLength => throw new DomainException(
                $"Entry display name cannot exceed {DisplayNameMaxLength} characters.",
                CompetitionErrorCodes.InvalidDisplayName),
            _ => trimmed
        };
    }

    private void EnsureActive()
    {
        if (Status != EntryStatus.Active)
        {
            throw new DomainException(
                $"Entry '{Id}' cannot change status from '{Status}'.",
                CompetitionErrorCodes.InvalidTransition);
        }
    }
}
