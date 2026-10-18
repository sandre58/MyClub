// -----------------------------------------------------------------------
// <copyright file="StructureHubNextStepDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Single hub-level structural next step for Structure PageHead info pill (fail-closed; null = no info step).
/// </summary>
/// <param name="Code">Stable action code (Overview / readiness segments; i18n under <c>structure.readiness.hubStep.*</c>).</param>
/// <param name="StageId">Target phase identity.</param>
/// <param name="StageName">Phase display name for label interpolation.</param>
public sealed record StructureHubNextStepDto(string Code, Guid StageId, string StageName);
