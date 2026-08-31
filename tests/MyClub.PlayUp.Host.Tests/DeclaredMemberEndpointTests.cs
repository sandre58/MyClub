// -----------------------------------------------------------------------
// <copyright file="DeclaredMemberEndpointTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Host.Contracts;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

[Collection("host-postgres")]
[Trait("Category", "Integration")]
public sealed class DeclaredMemberEndpointTests(HostPostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 31, 9, 30, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Declared_member_crud_returns_organisation_view_with_rosterAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedCompetitionWithEntryAsync(factory);
        using var client = factory.CreateClient();

        OrganisationViewDto? view;
        using (var add = await client.PostAsJsonAsync(
            DeclaredMembersUri(seed.CompetitionId, seed.EntryId),
            new AddDeclaredMemberRequest("Dupont", DeclaredMemberRole.Player)))
        {
            add.StatusCode.Should().Be(HttpStatusCode.OK);
            view = await add.Content.ReadFromJsonAsync<OrganisationViewDto>(HostJson.Options);
        }

        view.Should().NotBeNull();
        var entry = view!.Participants.Entries.Should().ContainSingle(e => e.EntryId == seed.EntryId.Value).Subject;
        entry.DeclaredMembers.Should().ContainSingle();
        var memberId = entry.DeclaredMembers![0].MemberId;
        entry.DeclaredMembers[0].DisplayName.Should().Be("Dupont");
        entry.DeclaredMembers[0].Role.Should().Be(DeclaredMemberRole.Player);

        using (var rename = await client.PostAsJsonAsync(
            RenameUri(seed.CompetitionId, seed.EntryId, memberId),
            new RenameDeclaredMemberRequest("Jean Dupont")))
        {
            rename.StatusCode.Should().Be(HttpStatusCode.OK);
            view = await rename.Content.ReadFromJsonAsync<OrganisationViewDto>(HostJson.Options);
        }

        view!.Participants.Entries.Single(e => e.EntryId == seed.EntryId.Value)
            .DeclaredMembers.Should().ContainSingle(m => m.DisplayName == "Jean Dupont");

        using (var changeRole = await client.PutAsJsonAsync(
            RoleUri(seed.CompetitionId, seed.EntryId, memberId),
            new ChangeDeclaredMemberRoleRequest(DeclaredMemberRole.Staff)))
        {
            changeRole.StatusCode.Should().Be(HttpStatusCode.OK);
            view = await changeRole.Content.ReadFromJsonAsync<OrganisationViewDto>(HostJson.Options);
        }

        view!.Participants.Entries.Single(e => e.EntryId == seed.EntryId.Value)
            .DeclaredMembers.Should().ContainSingle(m => m.Role == DeclaredMemberRole.Staff);

        using (var remove = await client.DeleteAsync(
            MemberUri(seed.CompetitionId, seed.EntryId, memberId)))
        {
            remove.StatusCode.Should().Be(HttpStatusCode.OK);
            view = await remove.Content.ReadFromJsonAsync<OrganisationViewDto>(HostJson.Options);
        }

        view!.Participants.Entries.Single(e => e.EntryId == seed.EntryId.Value)
            .DeclaredMembers.Should().BeEmpty();
    }

    [IntegrationFact]
    public async Task AddDeclaredMember_when_competition_missing_returns_404_CompetitionNotFoundAsync()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            DeclaredMembersUri(CompetitionId.New(), EntryId.New()),
            new AddDeclaredMemberRequest("Dupont", DeclaredMemberRole.Player));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem).Should().Be(ApplicationErrorCodes.CompetitionNotFound);
    }

    [IntegrationFact]
    public async Task RemoveDeclaredMember_when_referenced_by_match_sheet_returns_400Async()
    {
        await using var factory = new PlayUpWebApplicationFactory(fixture.ConnectionString);
        var seed = await SeedEntryWithMemberOnMatchSheetAsync(factory);
        using var client = factory.CreateClient();

        using var response = await client.DeleteAsync(
            MemberUri(seed.CompetitionId, seed.EntryId, seed.MemberId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(HostJson.Options);
        GetCode(problem).Should().Be(ApplicationErrorCodes.DeclaredMemberReferencedByMatchSheet);
    }

    private static Uri DeclaredMembersUri(CompetitionId competitionId, EntryId entryId) =>
        new($"/competitions/{competitionId.Value}/entries/{entryId.Value}/declared-members", UriKind.Relative);

    private static Uri MemberUri(CompetitionId competitionId, EntryId entryId, Guid memberId) =>
        new($"/competitions/{competitionId.Value}/entries/{entryId.Value}/declared-members/{memberId}", UriKind.Relative);

    private static Uri RenameUri(CompetitionId competitionId, EntryId entryId, Guid memberId) =>
        new($"/competitions/{competitionId.Value}/entries/{entryId.Value}/declared-members/{memberId}/rename", UriKind.Relative);

    private static Uri RoleUri(CompetitionId competitionId, EntryId entryId, Guid memberId) =>
        new($"/competitions/{competitionId.Value}/entries/{entryId.Value}/declared-members/{memberId}/role", UriKind.Relative);

    private static string? GetCode(ProblemDetails? problem) =>
        problem is null || !problem.Extensions.TryGetValue("code", out var raw) || raw is null
            ? null
            : raw switch
            {
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                _ => raw.ToString()
            };

    private async Task<(CompetitionId CompetitionId, EntryId EntryId)> SeedCompetitionWithEntryAsync(
        PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Roster Host Cup"), SampleRegulations.Standard(), _clock);
        var entry = competition.AddEntry(TeamId.New(), "FC Local", _clock);
        competitions.Add(competition);
        await unitOfWork.SaveChangesAsync();
        return (competition.Id, entry.Id);
    }

    private async Task<(CompetitionId CompetitionId, EntryId EntryId, Guid MemberId)> SeedEntryWithMemberOnMatchSheetAsync(
        PlayUpWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("R4 Host Cup"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Home", _clock);
        var away = competition.AddEntry(TeamId.New(), "Away", _clock);
        var member = competition.AddDeclaredMember(home.Id, "Dupont", DeclaredMemberRole.Player, _clock);
        competitions.Add(competition);

        var stage = Stage.Create(competition.Id, new StageName("Stage 1"), competition.Regulation, _clock);
        competition.AddStage(stage.Id, _clock);
        stages.Add(stage);

        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        match.AddDeclaredParticipation(member.Id, Side.Home, CompositionStatus.Starter, _clock);
        matches.Add(match);
        await unitOfWork.SaveChangesAsync();

        return (competition.Id, home.Id, member.Id.Value);
    }
}
