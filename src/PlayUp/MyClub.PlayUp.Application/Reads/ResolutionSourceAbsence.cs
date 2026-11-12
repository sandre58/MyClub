// -----------------------------------------------------------------------
// <copyright file="ResolutionSourceAbsence.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Why a stored path was recorded as <see cref="StructureResolutionIndeterminacyCause.SourceAbsente"/>.
/// A stage missing from the list passed to the detector is not, by itself, a deleted phase.
/// </summary>
[SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "Explicit absence details without an unknown sentinel.")]
public enum ResolutionSourceAbsence
{
    /// <summary>The source path id does not contain a stage identity.</summary>
    Unreadable = 1,

    /// <summary>The source id parses, and that stage was not in the stages given to the detector.</summary>
    StageNotLoaded = 2,

    /// <summary>The source stage is loaded, but the path, fixture, or round is gone.</summary>
    PathGone = 3,
}
