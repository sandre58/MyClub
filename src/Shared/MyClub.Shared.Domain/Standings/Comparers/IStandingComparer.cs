// -----------------------------------------------------------------------
// <copyright file="IStandingComparer.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

namespace MyClub.Shared.Domain.Standings.Comparers;

/// <summary>
/// Interface for comparing standing rows to determine their relative order in a standings table.
/// Extends the standard IComparer interface to provide type-safe comparison of standing rows.
/// Implementations define specific criteria for ranking teams, such as points, goal difference, head-to-head records, etc.
/// </summary>
public interface IStandingComparer : IComparer<IStandingRow>;
