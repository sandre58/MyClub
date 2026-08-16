// -----------------------------------------------------------------------
// <copyright file="WorkspaceSummaryAssemblerTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class WorkspaceSummaryAssemblerTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 16, 8, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assemble_draft_returns_continue_organisation_stub()
    {
        var competition = CreateCompetition.Execute("Draft Cup", _clock);

        var summary = WorkspaceSummaryAssembler.Assemble(competition);

        summary.Name.Should().Be("Draft Cup");
        summary.Status.Should().Be(CompetitionStatus.Draft);
        summary.NextActionCode.Should().Be(WorkspaceSummaryAssembler.ContinueOrganisationCode);
        summary.NextActionLabel.Should().Be("Continuer la préparation");
        summary.AttentionCount.Should().Be(0);
    }
}
