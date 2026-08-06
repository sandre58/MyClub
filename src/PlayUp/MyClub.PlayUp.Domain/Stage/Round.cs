// -----------------------------------------------------------------------
// <copyright file="Round.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stage;

/// <summary>
/// An elimination round within a stage (without fixtures in Phase 3.5).
/// </summary>
[DebuggerDisplay("{Name}")]
public sealed class Round : Entity<RoundId>
{
    /// <summary>
    /// Maximum allowed length of a round name after trim.
    /// </summary>
    public const int NameMaxLength = 100;

    internal Round(RoundId id, string name)
        : base(id) =>
        Name = NormalizeName(name);

    /// <summary>
    /// Gets the round display name.
    /// </summary>
    public string Name { get; private set; }

    internal void Rename(string name)
    {
        var normalized = NormalizeName(name);
        if (string.Equals(Name, normalized, StringComparison.Ordinal))
        {
            return;
        }

        Name = normalized;
    }

    private static string NormalizeName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var trimmed = name.Trim();
        return trimmed.Length switch
        {
            0 => throw new DomainException("Round name cannot be empty.", StageErrorCodes.InvalidDisplayName),
            > NameMaxLength => throw new DomainException(
                $"Round name cannot exceed {NameMaxLength} characters.",
                StageErrorCodes.InvalidDisplayName),
            _ => trimmed
        };
    }
}
