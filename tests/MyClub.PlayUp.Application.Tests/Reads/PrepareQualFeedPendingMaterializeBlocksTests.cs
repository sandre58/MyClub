// -----------------------------------------------------------------------
// <copyright file="PrepareQualFeedPendingMaterializeBlocksTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class PrepareQualFeedPendingMaterializeBlocksTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 13, 14, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Prepare_allows_ready_when_championship_structure_full_but_qual_feeders_pending()
    {
        var competition = Competition.Create(new CompetitionName("FormChamp"), SampleRegulations.Standard(), _clock);
        var source = Stage.Create(
            competition.Id,
            new StageName("Poules"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: true),
            _clock);
        competition.AddStage(source.Id, _clock);
        source.AddGroup("A", _clock);
        source.AddGroup("B", _clock);
        source.AddMatchday(1, _clock);

        var champ = Stage.Create(competition.Id, new StageName("Championnat"), SampleRegulations.Standard(), _clock);
        champ.AddMatchday(1, _clock);
        competition.AddStage(champ.Id, _clock);
        var e1 = competition.AddEntry(TeamId.New(), "E1", _clock).Id;
        var e2 = competition.AddEntry(TeamId.New(), "E2", _clock).Id;
        champ.ReplaceAffectationAuthoring([e1, e2], _clock);

        ReplaceStageQualificationRules.Execute(
            source,
            [
                new QualificationIntentSpec(
                    Guid.NewGuid(),
                    1,
                    QualificationIntentSourceKind.EachGroup,
                    1,
                    1,
                    champ.Id.Value,
                    DestinationForm: true)
            ],
            _clock);

        champ.CompositionEntries.Should().HaveCount(2);
        PrepareStage.Execute(champ, [source, champ], _clock);
        champ.Status.Should().Be(StageStatus.Ready);

        var act = () => MaterializeMatches.Execute(competition, champ, [], _clock, [source, champ]);
        act.Should().Throw<ApplicationFailureException>();
    }
}
