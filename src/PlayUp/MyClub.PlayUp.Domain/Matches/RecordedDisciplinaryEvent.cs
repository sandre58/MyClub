// -----------------------------------------------------------------------
// <copyright file="RecordedDisciplinaryEvent.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Domain.Matches;

/// <summary>
/// Nominative disciplinary fact recorded on a match — no Domain consequences
/// (presence, score, result, substitutions, standing, suspensions).
/// </summary>
[DebuggerDisplay("{Type} → {MemberId} ({Id})")]
public sealed class RecordedDisciplinaryEvent : Entity<DisciplinaryEventId>
{
    internal RecordedDisciplinaryEvent(DisciplinaryEventId id, MemberId memberId, DisciplinaryType type)
        : base(id)
    {
        EnsureDefinedType(type);
        MemberId = memberId;
        Type = type;
    }

    /// <summary>
    /// Gets the targeted member (must be on the match composition).
    /// </summary>
    public MemberId MemberId { get; private set; }

    /// <summary>
    /// Gets the disciplinary type from the closed catalogue.
    /// </summary>
    public DisciplinaryType Type { get; private set; }

    internal void Correct(MemberId memberId, DisciplinaryType type)
    {
        EnsureDefinedType(type);
        MemberId = memberId;
        Type = type;
    }

    private static void EnsureDefinedType(DisciplinaryType type)
    {
        if (!Enum.IsDefined(type))
        {
            throw new DomainException(
                $"Unknown disciplinary type '{type}'.",
                MatchErrorCodes.InvalidDisciplinaryType);
        }
    }
}
