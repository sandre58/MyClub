// -----------------------------------------------------------------------
// <copyright file="ConfigureStructureResult.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Competitions;

/// <summary>
/// Result of <see cref="ConfigureStructure"/> (caller persists Stage when newly created).
/// </summary>
/// <param name="Stage">Primary stage after structure mutation.</param>
/// <param name="StageCreated">True when the stage was created in this call.</param>
public sealed record ConfigureStructureResult(Stage Stage, bool StageCreated);
