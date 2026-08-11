// -----------------------------------------------------------------------
// <copyright file="UbiquitousLanguageEnumTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stage;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Common;

public sealed class UbiquitousLanguageEnumTests
{
    [Fact]
    public void CompetitionStatus_matches_documented_lifecycle() =>
        Enum.GetNames<CompetitionStatus>().Should().BeEquivalentTo("Draft", "Ready", "Running", "Suspended", "Completed", "Archived");

    [Fact]
    public void StageStatus_matches_documented_lifecycle() =>
        Enum.GetNames<StageStatus>().Should().BeEquivalentTo("Draft", "Ready", "Running", "Suspended", "Completed");

    [Fact]
    public void DrawStatus_matches_documented_lifecycle() =>
        Enum.GetNames<DrawStatus>().Should().BeEquivalentTo("Draft", "Published", "Cancelled");

    [Fact]
    public void DrawMode_matches_documented_V1_execution_modes() =>
        Enum.GetNames<DrawMode>().Should().BeEquivalentTo("Random");

    [Fact]
    public void DrawResolutionKind_matches_documented_kinds() =>
        Enum.GetNames<DrawResolutionKind>().Should().BeEquivalentTo("Slot", "Group", "Pairing");

    [Fact]
    public void DrawResolutionState_is_orthogonal_to_lifecycle() =>
        Enum.GetNames<DrawResolutionState>().Should().BeEquivalentTo("NotResolved", "Resolved", "NoSolution");

    [Fact]
    public void MatchStatus_matches_documented_lifecycle() =>
        Enum.GetNames<MatchStatus>().Should().BeEquivalentTo("Scheduled", "Live", "Finished", "Postponed", "Cancelled");

    [Fact]
    public void EntryStatus_matches_documented_values() =>
        Enum.GetNames<EntryStatus>().Should().BeEquivalentTo("Active", "Qualified", "Eliminated", "Withdrawn", "Excluded");

    [Fact]
    public void ResultType_matches_documented_values() =>
        Enum.GetNames<ResultType>().Should().BeEquivalentTo("Played", "Forfeit", "WalkOver", "Administrative");

    [Fact]
    public void CompletionMode_matches_documented_values() =>
        Enum.GetNames<CompletionMode>().Should().BeEquivalentTo("Normal", "Administrative", "Abandoned");
}
