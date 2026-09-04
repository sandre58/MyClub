// -----------------------------------------------------------------------
// <copyright file="DeclaredMember.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Competitions;

/// <summary>
/// A person declared in the context of a <see cref="CompetitionEntry"/>.
/// Identity (<see cref="MemberId"/>) is local to that participation declaration — not a global person identity.
/// </summary>
[DebuggerDisplay("{DisplayName} ({Role})")]
public sealed class DeclaredMember : Entity<MemberId>
{
    /// <summary>
    /// Maximum allowed length of a member display name after trim.
    /// </summary>
    public const int DisplayNameMaxLength = 100;

    internal DeclaredMember(MemberId id, string displayName, DeclaredMemberRole role)
        : base(id)
    {
        EnsureDefinedRole(role);
        DisplayName = NormalizeDisplayName(displayName);
        Role = role;
    }

    /// <summary>
    /// Gets the display name for this declared member.
    /// </summary>
    public string DisplayName { get; private set; }

    /// <summary>
    /// Gets the declared role within this participation.
    /// </summary>
    public DeclaredMemberRole Role { get; private set; }

    internal void Rename(string displayName) => DisplayName = NormalizeDisplayName(displayName);

    private static string NormalizeDisplayName(string displayName)
    {
        ArgumentNullException.ThrowIfNull(displayName);
        var trimmed = displayName.Trim();
        return trimmed.Length switch
        {
            0 => throw new DomainException(
                "Member display name cannot be empty.",
                CompetitionErrorCodes.InvalidMemberDisplayName),
            > DisplayNameMaxLength => throw new DomainException(
                $"Member display name cannot exceed {DisplayNameMaxLength} characters.",
                CompetitionErrorCodes.InvalidMemberDisplayName),
            _ => trimmed
        };
    }

    private static void EnsureDefinedRole(DeclaredMemberRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new DomainException(
                $"Unknown declared member role '{role}'.",
                CompetitionErrorCodes.InvalidMemberRole);
        }
    }
}
