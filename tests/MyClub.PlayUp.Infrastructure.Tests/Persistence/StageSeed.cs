// -----------------------------------------------------------------------
// <copyright file="StageSeed.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.Tests.Common;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

internal static class StageSeed
{
    internal static Stage CreateDraft(
        CompetitionId competitionId,
        IClock clock,
        string name = "Stage") =>
        Stage.Create(competitionId, new StageName(name), SampleRegulations.Standard(), clock);
}
