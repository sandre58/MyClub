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

    private readonly List<DeclaredMember> _declaredMembers = [];

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
    /// Gets the abbreviated name (required after add / presentation update).
    /// </summary>
    public ShortName? ShortName { get; private set; }

    /// <summary>
    /// Gets the optional Media reference for the entry logo.
    /// </summary>
    public LogoMediaId? LogoMediaId { get; private set; }

    /// <summary>
    /// Gets the optional primary kit color.
    /// </summary>
    public TeamColor? PrimaryColor { get; private set; }

    /// <summary>
    /// Gets the optional secondary kit color.
    /// </summary>
    public TeamColor? SecondaryColor { get; private set; }

    /// <summary>
    /// Gets the participation status.
    /// </summary>
    public EntryStatus Status { get; private set; }

    /// <summary>
    /// Gets the members declared for this participation.
    /// </summary>
    public IReadOnlyList<DeclaredMember> DeclaredMembers => _declaredMembers.AsReadOnly();

    internal void Rename(string displayName) => DisplayName = NormalizeDisplayName(displayName);

    internal void UpdatePresentation(EntryPresentation presentation)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        ApplyPresentation(presentation);
    }

    internal void Withdraw()
    {
        EnsureActive();
        Status = EntryStatus.Withdrawn;
    }

    internal DeclaredMember AddDeclaredMember(MemberId memberId, string displayName, DeclaredMemberRole role)
    {
        if (_declaredMembers.Exists(member => member.Id.Equals(memberId)))
        {
            throw new DomainException(
                $"Declared member '{memberId}' already exists on entry '{Id}'.",
                CompetitionErrorCodes.DuplicateMember);
        }

        var member = new DeclaredMember(memberId, displayName, role);
        _declaredMembers.Add(member);
        return member;
    }

    internal DeclaredMember GetDeclaredMember(MemberId memberId) =>
        _declaredMembers.FirstOrDefault(member => member.Id.Equals(memberId))
        ?? throw new DomainException(
            $"Declared member '{memberId}' was not found on entry '{Id}'.",
            CompetitionErrorCodes.MemberNotFound);

    internal void RemoveDeclaredMember(MemberId memberId)
    {
        var member = GetDeclaredMember(memberId);
        _declaredMembers.Remove(member);
    }

    internal void RenameDeclaredMember(MemberId memberId, string displayName) =>
        GetDeclaredMember(memberId).Rename(displayName);

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

    private void ApplyPresentation(EntryPresentation presentation)
    {
        ShortName = presentation.ShortName;
        LogoMediaId = presentation.LogoMediaId;
        PrimaryColor = presentation.PrimaryColor;
        SecondaryColor = presentation.SecondaryColor;
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
