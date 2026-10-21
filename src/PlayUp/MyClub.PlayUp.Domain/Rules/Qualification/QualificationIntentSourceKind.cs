// -----------------------------------------------------------------------
// <copyright file="QualificationIntentSourceKind.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Authoring source shape for a <see cref="QualificationIntent"/>.
/// </summary>
public enum QualificationIntentSourceKind
{
    /// <summary>One specific group (GroupId required).</summary>
    SingleGroup = 0,

    /// <summary>Every group of the source stage (GroupOrder × positions).</summary>
    EachGroup = 1,

    /// <summary>Overall stage ranking.</summary>
    Overall = 2,

    /// <summary>Across-groups ranking for a fixed place P.</summary>
    AcrossGroups = 3
}
