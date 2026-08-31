// -----------------------------------------------------------------------
// <copyright file="OrganisationRequestMapperRegulationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

public sealed class OrganisationRequestMapperRegulationTests
{
    [Fact]
    public void ToRegulation_omitted_AllowedTypes_preserves_existing_disciplinary_rules()
    {
        var existing = new DisciplinaryRules([DisciplinaryType.Yellow, DisciplinaryType.White]);
        var request = new ReplaceRegulationRequest(
            MinimumTeams: 2,
            MaximumTeams: 64,
            DurationPerPeriod: 45,
            NumberOfPeriods: 2,
            HalfTimeDuration: 15,
            WinPoints: 3,
            DrawPoints: 1,
            LossPoints: 0);

        var regulation = OrganisationRequestMapper.ToRegulation(request, existing);

        regulation.DisciplinaryRules.AllowedTypes.Should().BeEquivalentTo(
            [DisciplinaryType.Yellow, DisciplinaryType.White]);
    }

    [Fact]
    public void ToRegulation_empty_AllowedTypes_sets_None()
    {
        var existing = new DisciplinaryRules([DisciplinaryType.Red]);
        var request = new ReplaceRegulationRequest(
            MinimumTeams: 2,
            MaximumTeams: 64,
            DurationPerPeriod: 45,
            NumberOfPeriods: 2,
            HalfTimeDuration: 15,
            WinPoints: 3,
            DrawPoints: 1,
            LossPoints: 0,
            AllowedTypes: []);

        var regulation = OrganisationRequestMapper.ToRegulation(request, existing);

        regulation.DisciplinaryRules.Should().Be(DisciplinaryRules.None);
    }

    [Fact]
    public void ToRegulation_explicit_AllowedTypes_replaces_catalogue()
    {
        var existing = DisciplinaryRules.None;
        var request = new ReplaceRegulationRequest(
            MinimumTeams: 2,
            MaximumTeams: 64,
            DurationPerPeriod: 45,
            NumberOfPeriods: 2,
            HalfTimeDuration: 15,
            WinPoints: 3,
            DrawPoints: 1,
            LossPoints: 0,
            AllowedTypes: [DisciplinaryType.Yellow, DisciplinaryType.Red]);

        var regulation = OrganisationRequestMapper.ToRegulation(request, existing);

        regulation.DisciplinaryRules.AllowedTypes.Should().BeEquivalentTo(
            [DisciplinaryType.Yellow, DisciplinaryType.Red]);
    }
}
