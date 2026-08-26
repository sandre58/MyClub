// -----------------------------------------------------------------------
// <copyright file="CompetitionPresentationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Competitions.Events;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Competitions;

public sealed class CompetitionPresentationTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void UpdatePresentation_sets_short_name_and_logo()
    {
        var competition = CreateDraft();

        competition.UpdatePresentation(ShortName.Create("L1"), LogoUri.Create("/seed-logos/ligue-1/comp.png"), _clock);

        competition.ShortName!.Value.Should().Be("L1");
        competition.LogoUri!.Value.Should().Be("/seed-logos/ligue-1/comp.png");
        competition.DomainEvents.OfType<CompetitionPresentationUpdated>().Should().ContainSingle();
    }

    [Fact]
    public void SetSchedule_rejects_start_after_end()
    {
        var competition = CreateDraft();
        var start = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

        var act = () => competition.SetSchedule(start, end, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidSchedule);
    }

    [Fact]
    public void SetSchedule_allows_partial_or_both_ordered()
    {
        var competition = CreateDraft();
        var start = new DateTimeOffset(2026, 8, 15, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2027, 5, 30, 0, 0, 0, TimeSpan.Zero);

        competition.SetSchedule(start, null, _clock);
        competition.ScheduledStart.Should().Be(start);
        competition.ScheduledEnd.Should().BeNull();

        competition.SetSchedule(start, end, _clock);
        competition.ScheduledEnd.Should().Be(end);

        competition.SetSchedule(null, null, _clock);
        competition.ScheduledStart.Should().BeNull();
        competition.ScheduledEnd.Should().BeNull();
    }

    [Fact]
    public void AddEntry_accepts_presentation()
    {
        var competition = CreateDraft();
        var presentation = new EntryPresentation(
            ShortName.Create("PSG"),
            LogoUri.Create("/seed-logos/ligue-1/psg.png"),
            TeamColor.Create("#004170"),
            TeamColor.Create("#DA291C"));

        var entry = competition.AddEntry(TeamId.New(), "Paris Saint-Germain", _clock, presentation);

        entry.ShortName!.Value.Should().Be("PSG");
        entry.LogoUri!.Value.Should().Be("/seed-logos/ligue-1/psg.png");
        entry.PrimaryColor!.Value.Should().Be("#004170");
        entry.SecondaryColor!.Value.Should().Be("#DA291C");
    }

    [Fact]
    public void UpdateEntryPresentation_replaces_fields()
    {
        var competition = CreateDraft();
        var entry = competition.AddEntry(TeamId.New(), "Alpha", _clock, new EntryPresentation(ShortName.Create("ALP")));

        competition.UpdateEntryPresentation(
            entry.Id,
            new EntryPresentation(null, LogoUri.Create("https://example.com/a.png"), TeamColor.Create("#AABBCC")),
            _clock);

        entry.ShortName.Should().BeNull();
        entry.LogoUri!.Value.Should().Be("https://example.com/a.png");
        entry.PrimaryColor!.Value.Should().Be("#AABBCC");
        entry.SecondaryColor.Should().BeNull();
    }

    [Fact]
    public void Presentation_mutations_require_draft_or_ready()
    {
        var competition = CreateDraft();
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddStage(StageId.New(), _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var act = () => competition.UpdatePresentation(ShortName.Create("X"), null, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidTransition);
    }

    [Theory]
    [InlineData("not-a-path")]
    [InlineData("ftp://example.com/a.png")]
    public void LogoUri_rejects_invalid(string raw)
    {
        var act = () => LogoUri.Create(raw);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(CompetitionErrorCodes.InvalidLogoUri);
    }

    [Fact]
    public void TeamColor_normalizes_uppercase() => TeamColor.Create("#aAbBcC")!.Value.Should().Be("#AABBCC");

    private Competition CreateDraft() =>
        Competition.Create(new CompetitionName("Meta Cup"), SampleRegulations.Standard(), _clock);
}
