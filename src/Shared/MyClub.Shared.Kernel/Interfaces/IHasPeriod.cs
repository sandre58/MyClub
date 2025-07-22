// -----------------------------------------------------------------------
// <copyright file="IHasPeriod.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyNet.Utilities.DateTimes;

namespace MyClub.Shared.Kernel.Interfaces;

public interface IHasPeriod
{
    Period Period { get; }
}
