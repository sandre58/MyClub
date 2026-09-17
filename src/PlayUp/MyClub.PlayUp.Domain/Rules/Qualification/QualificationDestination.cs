// -----------------------------------------------------------------------
// <copyright file="QualificationDestination.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Where selected participants are routed: destination stage population only (Qual V2 / I4).
/// Does not express form placement (slot) — that is Placement / Draw.
/// </summary>
public sealed record QualificationDestination
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QualificationDestination"/> class.
    /// </summary>
    /// <param name="stageId">The destination stage whose population receives the entry.</param>
    public QualificationDestination(StageId stageId) => StageId = stageId;

    /// <summary>
    /// Creates a population-targeting destination (StageId only).
    /// </summary>
    public static QualificationDestination ForPopulation(StageId stageId) => new(stageId);

    /// <summary>
    /// Gets the destination stage identity.
    /// </summary>
    public StageId StageId { get; }
}
