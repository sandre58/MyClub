// -----------------------------------------------------------------------
// <copyright file="BindStageRegulationRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for rebinding Match or Standing heritable parts to Competition defaults.
/// <see cref="Scope"/>: <c>Match</c> or <c>Standing</c>.
/// </summary>
public sealed record BindStageRegulationRequest(string Scope);
