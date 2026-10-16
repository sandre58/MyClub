// -----------------------------------------------------------------------
// <copyright file="StructureInboundCompositionAssemblerTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class StructureInboundCompositionAssemblerTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assemble_projects_qualification_path_per_inbound_rule()
    {
        var competition = Competition.Create(
            new CompetitionName("Qual inbound"),
            SampleRegulations.Standard(),
            _clock);
        var source = Stage.Create(competition.Id, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var final = Stage.Create(competition.Id, new StageName("Final"), SampleRegulations.Standard(), _clock);
        competition.AddStage(source.Id, _clock);
        competition.AddStage(final.Id, _clock);
        var groupA = source.AddGroup("Poule A", _clock);

        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.FromGroup(groupA.Id),
                    new QualificationSelection(SelectionMode.Range, 1, 2),
                    QualificationDestination.ForPopulation(final.Id)),
            ]),
            _clock);

        var inbound = StructureInboundCompositionAssembler.Assemble(
            competition,
            final,
            [source, final]);

        inbound.Should().NotBeNull();
        inbound!.Rules.Should().ContainSingle();
        var rule = inbound.Rules[0];
        rule.QualificationPath.Should().NotBeNull();
        rule.QualificationPath!.Order.Should().Be(1);
        rule.QualificationPath.SelectionMode.Should().Be(SelectionMode.Range);
        rule.QualificationPath.SelectionValue.Should().Be(1);
        rule.QualificationPath.SelectionEndValue.Should().Be(2);
        rule.QualificationPath.GroupName.Should().Be("Poule A");
        rule.QualificationPath.DestinationStageId.Should().Be(final.Id.Value);
        rule.ProgressionPath.Should().BeNull();
    }
}
