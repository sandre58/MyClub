// -----------------------------------------------------------------------
// <copyright file="StageSummaryRow.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Abstractions;

/// <summary>
/// Slim read projection for competition detail stage summaries.
/// </summary>
/// <param name="Id">Stage identity.</param>
/// <param name="Name">Display name.</param>
/// <param name="Status">Lifecycle status.</param>
public sealed record StageSummaryRow(StageId Id, string Name, StageStatus Status);
