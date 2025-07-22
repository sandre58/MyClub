// -----------------------------------------------------------------------
// <copyright file="IMatchdaysGeneratorService.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Shared.Domain.Teams;

namespace MyClub.Scorer.Application.Matchdays.Services;

public interface IMatchdaysGeneratorService
{
    IReadOnlyCollection<Matchday> GenerateMatchdays(IReadOnlyCollection<TeamReference> teams);
}

public record GeneraredMatchday(Matchday Matchday, IReadOnlyCollection<Match> Matches);
