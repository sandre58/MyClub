// -----------------------------------------------------------------------
// <copyright file="IMatchdayNameProvider.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.Scorer.Application.Matchdays.Services;

public interface IMatchdayNameProvider
{
    string GetMatchdayName(int index);
}
