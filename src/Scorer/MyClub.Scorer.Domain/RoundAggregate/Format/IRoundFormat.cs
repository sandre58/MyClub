// -----------------------------------------------------------------------
// <copyright file="IRoundFormat.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Scorer.Domain.RoundAggregate.Format;

public interface IRoundFormat
{
    bool AllowDraw();
}
