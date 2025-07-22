// -----------------------------------------------------------------------
// <copyright file="PeriodFormat.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyNet.Utilities;

namespace MyClub.Scorer.Domain.CompetitionAggregate.Configurations;

public record PeriodFormat(int Number, TimeSpan Duration, TimeSpan? HalfTimeDuration = null)
{
    public static readonly PeriodFormat Default = new(2, 45.Minutes(), 15.Minutes());

    public static readonly PeriodFormat ExtraTime = new(2, 15.Minutes(), 5.Minutes());

    public TimeSpan GetFullTime(bool withHalfTime = true) => (Number * Duration) + ((Number - 1) * (withHalfTime && HalfTimeDuration.HasValue ? HalfTimeDuration.Value : TimeSpan.Zero));

    public override string ToString() => $"{Number}x{Duration.TotalMinutes}'";
}
