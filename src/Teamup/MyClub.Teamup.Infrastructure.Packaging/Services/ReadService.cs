// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyNet.Utilities;
using MyNet.Utilities.IO.FileExtensions;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Progress;
using MyClub.Teamup.Application.Contracts;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.SquadAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyClub.Teamup.Infrastructure.Packaging.Converters;
using MyClub.Teamup.Infrastructure.Packaging.Models;
using MyClub.CrossCutting.Packaging;

namespace MyClub.Teamup.Infrastructure.Packaging.Services
{
    public sealed class ReadService : IReadService
    {
        public bool CanRead(string filename) => TmprojFileExtensionInfo.Tmproj.IsValid(filename);

        public async Task<Project> ReadAsync(string filename, CancellationToken? token = null)
        {
            var package = await ReadAsync(filename, ReaderOptions.ReadAll, token).ConfigureAwait(false);

            using (LogManager.MeasureTime("Convert Project"))
            using (ProgressManager.Start())
                return package.CreateProject();
        }

        public async Task<byte[]?> ReadImageAsync(string filename, CancellationToken? token = null)
        {
            using var reader = new PackageReader(filename);
            var metadata = await ReadMetadataAsync(reader, token).ConfigureAwait(false);

            return metadata?.Image;
        }

        public async Task<IEnumerable<SquadPlayer>> ReadSquadPlayersAsync(string filename, CancellationToken? token = null)
        {
            var package = await ReadAsync(filename, ReaderOptions.ReadSquads | ReaderOptions.ReadPlayers, token).ConfigureAwait(false);

            var players = package.Players?.Select(x => x.CreatePlayer()).ToArray() ?? [];
            var squadPlayers = package.Squads?.FirstOrDefault()?.Players?.Select(x => x.CreateSquadPlayer(players.GetById(x.PlayerId))).ToArray();

            return squadPlayers ?? [];
        }

        public async Task<IEnumerable<Team>> ReadTeamsAsync(string filename, CancellationToken? token = null)
        {
            var package = await ReadAsync(filename, ReaderOptions.ReadMetadata | ReaderOptions.ReadStadiums | ReaderOptions.ReadClubs, token).ConfigureAwait(false);

            var stadiums = package.Stadiums?.Select(x => x.CreateStadium()).ToArray() ?? [];
            var clubs = package.Clubs?.Where(x => x.Id != package.Metadata!.ClubId).Select(x => x.CreateClub(stadiums)).ToArray() ?? [];
            var teams = clubs.SelectMany(x => x.Teams).ToArray();

            return teams;
        }

        public async Task<IEnumerable<ICompetition>> ReadCompetitionsAsync(string filename, CancellationToken? token = null)
        {
            var package = await ReadAsync(filename, ReaderOptions.ReadCompetitions, token).ConfigureAwait(false);

            var leagues = package.Competitions?.Leagues?.Select(x => x.CreateLeague()).ToArray() ?? [];
            var cups = package.Competitions?.Cups?.Select(x => x.CreateCup()).ToArray() ?? [];
            var friendlies = package.Competitions?.Friendlies?.Select(x => x.CreateFriendly()).ToArray() ?? [];

            return leagues.Concat(cups.OfType<ICompetition>()).Concat(friendlies.OfType<ICompetition>());
        }

        private static async Task<ProjectPackage> ReadAsync(string filename, ReaderOptions options, CancellationToken? token = null)
        {
            using (ProgressManager.Start(options.CountFlags()))
            {
                using var reader = new PackageReader(filename);

                var projectModel = new ProjectPackage();

                if (options.HasFlag(ReaderOptions.ReadMetadata)) projectModel.Metadata = await ReadMetadataAsync(reader, token).ConfigureAwait(false);
                token?.ThrowIfCancellationRequested();
                if (options.HasFlag(ReaderOptions.ReadMetadata)) projectModel.Seasons = await ReadSeasonsAsync(reader, token).ConfigureAwait(false);
                token?.ThrowIfCancellationRequested();
                if (options.HasFlag(ReaderOptions.ReadPlayers)) projectModel.Players = await ReadPlayersAsync(reader, token).ConfigureAwait(false);
                token?.ThrowIfCancellationRequested();
                if (options.HasFlag(ReaderOptions.ReadSquads)) projectModel.Squads = await ReadSquadsAsync(reader, token).ConfigureAwait(false);
                token?.ThrowIfCancellationRequested();
                if (options.HasFlag(ReaderOptions.ReadTrainingSessions)) projectModel.TrainingSessions = await ReadTrainingSessionsAsync(reader, token).ConfigureAwait(false);
                token?.ThrowIfCancellationRequested();
                if (options.HasFlag(ReaderOptions.ReadSendedMails)) projectModel.SendedMails = await ReadSendedMailsAsync(reader, token).ConfigureAwait(false);
                token?.ThrowIfCancellationRequested();
                if (options.HasFlag(ReaderOptions.ReadHolidays)) projectModel.Holidays = await ReadHolidaysAsync(reader, token).ConfigureAwait(false);
                token?.ThrowIfCancellationRequested();
                if (options.HasFlag(ReaderOptions.ReadCycles)) projectModel.Cycles = await ReadCyclesAsync(reader, token).ConfigureAwait(false);
                token?.ThrowIfCancellationRequested();
                if (options.HasFlag(ReaderOptions.ReadTactics)) projectModel.Tactics = await ReadTacticsAsync(reader, token).ConfigureAwait(false);
                token?.ThrowIfCancellationRequested();
                if (options.HasFlag(ReaderOptions.ReadStadiums)) projectModel.Stadiums = await ReadStadiumsAsync(reader, token).ConfigureAwait(false);
                token?.ThrowIfCancellationRequested();
                if (options.HasFlag(ReaderOptions.ReadClubs)) projectModel.Clubs = await ReadClubsAsync(reader, token).ConfigureAwait(false);
                token?.ThrowIfCancellationRequested();
                if (options.HasFlag(ReaderOptions.ReadCompetitions)) projectModel.Competitions = await ReadCompetitionsAsync(reader, token).ConfigureAwait(false);
                token?.ThrowIfCancellationRequested();

                return projectModel;
            }
        }

        private static async Task<MetadataPackage?> ReadMetadataAsync(PackageReader reader, CancellationToken? token)
        {
            var metadata = await reader.ReadAsync<MetadataPackage>(XmlConstants.MetadataUri, token).ConfigureAwait(false);

            if (metadata is not null)
                metadata.Image = await reader.ReadImageAsync(XmlConstants.ProjectImageUri, token).ConfigureAwait(false);

            return metadata;
        }


        private static async Task<PlayersPackage?> ReadPlayersAsync(PackageReader reader, CancellationToken? token = null)
        {
            using var step = ProgressManager.Start();
            var result = await reader.ReadStreamAsync<PlayersPackage>(XmlConstants.PlayersUri, token).ConfigureAwait(false);

            var i = 0;
            if (result is not null)
            {
                foreach (var p in result)
                {
                    token?.ThrowIfCancellationRequested();
                    p.Photo = await reader.ReadImageAsync(XmlConstants.PlayerPhotoUri.FormatWith(p.Id), token).ConfigureAwait(false);

                    i++;

                    step?.UpdateProgress((double)i / result.Count);
                }
            }

            return result;
        }

        private static async Task<ClubsPackage?> ReadClubsAsync(PackageReader reader, CancellationToken? token = null)
        {
            using var step = ProgressManager.Start();
            var result = await reader.ReadStreamAsync<ClubsPackage>(XmlConstants.ClubsUri, token).ConfigureAwait(false);

            var i = 0;
            if (result is not null)
            {
                foreach (var p in result)
                {
                    token?.ThrowIfCancellationRequested();
                    p.Logo = await reader.ReadImageAsync(XmlConstants.ClubLogoUri.FormatWith(p.Id), token).ConfigureAwait(false);

                    i++;

                    step?.UpdateProgress((double)i / result.Count);
                }
            }

            return result;
        }

        private static async Task<CompetitionsPackage?> ReadCompetitionsAsync(PackageReader reader, CancellationToken? token = null)
        {
            using var step = ProgressManager.Start();
            var result = await reader.ReadStreamAsync<CompetitionsPackage>(XmlConstants.CompetitionsUri, token).ConfigureAwait(false);

            if (result is not null)
            {
                var i = 0;

                var competitions = result.Cups?.Concat(result.Leagues?.ToArray() ?? Array.Empty<CompetitionPackage>())
                                               .Concat(result.Friendlies?.ToArray() ?? Array.Empty<CompetitionPackage>())
                                               .ToList() ?? [];

                foreach (var p in competitions)
                {
                    token?.ThrowIfCancellationRequested();
                    p.Logo = await reader.ReadImageAsync(XmlConstants.CompetitionLogoUri.FormatWith(p.Id), token).ConfigureAwait(false);

                    i++;

                    step?.UpdateProgress((double)i / competitions.Count);
                }
            }

            return result;
        }

        private static async Task<SeasonsPackage?> ReadSeasonsAsync(PackageReader reader, CancellationToken? token = null)
        {
            using (ProgressManager.Start())
                return await reader.ReadStreamAsync<SeasonsPackage>(XmlConstants.SeasonsUri, token).ConfigureAwait(false);

        }

        private static async Task<TrainingSessionsPackage?> ReadTrainingSessionsAsync(PackageReader reader, CancellationToken? token = null)
        {
            using (ProgressManager.Start())
                return await reader.ReadStreamAsync<TrainingSessionsPackage>(XmlConstants.TrainingSessionsUri, token).ConfigureAwait(false);
        }

        private static async Task<StadiumsPackage?> ReadStadiumsAsync(PackageReader reader, CancellationToken? token = null)
        {
            using (ProgressManager.Start())
                return await reader.ReadStreamAsync<StadiumsPackage>(XmlConstants.StadiumsUri, token).ConfigureAwait(false);
        }

        private static async Task<SquadsPackage?> ReadSquadsAsync(PackageReader reader, CancellationToken? token = null)
        {
            using (ProgressManager.Start())
                return await reader.ReadStreamAsync<SquadsPackage>(XmlConstants.SquadsUri, token).ConfigureAwait(false);
        }

        private static async Task<TacticsPackage?> ReadTacticsAsync(PackageReader reader, CancellationToken? token = null)
        {
            using (ProgressManager.Start())
                return await reader.ReadStreamAsync<TacticsPackage>(XmlConstants.TacticsUri, token).ConfigureAwait(false);
        }

        private static async Task<SendedMailsPackage?> ReadSendedMailsAsync(PackageReader reader, CancellationToken? token = null)
        {
            using (ProgressManager.Start())
                return await reader.ReadStreamAsync<SendedMailsPackage>(XmlConstants.SendedMailsUri, token).ConfigureAwait(false);
        }

        private static async Task<HolidaysPackage?> ReadHolidaysAsync(PackageReader reader, CancellationToken? token = null)
        {
            using (ProgressManager.Start())
                return await reader.ReadStreamAsync<HolidaysPackage>(XmlConstants.HolidaysUri, token).ConfigureAwait(false);
        }

        private static async Task<CyclesPackage?> ReadCyclesAsync(PackageReader reader, CancellationToken? token = null)
        {
            using (ProgressManager.Start())
                return await reader.ReadStreamAsync<CyclesPackage>(XmlConstants.CyclesUri, token).ConfigureAwait(false);
        }

    }
}
