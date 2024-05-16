// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyNet.Utilities;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Helpers;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Progress;
using MyClub.Teamup.Application.Contracts;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Infrastructure.Packaging.Converters;
using MyClub.Teamup.Infrastructure.Packaging.Models;
using MyClub.CrossCutting.Packaging;
using MyClub.CrossCutting.FileSystem;

namespace MyClub.Teamup.Infrastructure.Packaging.Services
{
    public sealed class WriteService(ITempService tempService) : IWriteService
    {
        private const long HResultNotEnoughFreeSpaceOnDiskError = -2147024784;

        private readonly ITempService _tempService = tempService;

        public async Task<bool> WriteAsync(Project project, string filename, CancellationToken? token = null)
            => await WriteAsync(project, filename, WriterOptions.WriteAll, token).ConfigureAwait(false);

        public async Task<bool> WriteAsync(Project project, string filename, WriterOptions options, CancellationToken? token = null)
        {
            if (string.IsNullOrEmpty(filename))
                throw new InvalidOperationException("File path is empty.");

            using (LogManager.MeasureTime($"Save file : {filename}", TraceLevel.Debug))
            {
                var tmpFile = _tempService.CreateFile();

                try
                {
                    ProjectPackage package = null!;
                    using (LogManager.MeasureTime("Convert Project"))
                        package = project.ToPackage();

                    using (LogManager.MeasureTime($"Save temp file : {tmpFile}", TraceLevel.Debug))
                    using (ProgressManager.Start(options.CountFlags()))
                    {
                        using var writer = new PackageWriter(tmpFile);

                        if (package.Metadata is not null && options.HasFlag(WriterOptions.WriteMetadata))
                        {
                            await writer.WriteAsync(package.Metadata, XmlConstants.MetadataUri, token).ConfigureAwait(false);

                            if (package.Metadata.Image is not null)
                                await writer.WriteImageAsync(package.Metadata.Image, XmlConstants.ProjectImageUri, token).ConfigureAwait(false);
                        }
                        if (package.Seasons is not null && options.HasFlag(WriterOptions.WriteMetadata)) await WriteSeasonsAsync(writer, package.Seasons, token).ConfigureAwait(false);
                        if (package.Players is not null && options.HasFlag(WriterOptions.WritePlayers)) await WritePlayersAsync(writer, package.Players, token).ConfigureAwait(false);
                        if (package.Squads is not null && options.HasFlag(WriterOptions.WriteSquads)) await WriteSquadsAsync(writer, package.Squads, token).ConfigureAwait(false);
                        if (package.TrainingSessions is not null && options.HasFlag(WriterOptions.WriteTrainingSessions)) await WriteTrainingSessionsAsync(writer, package.TrainingSessions, token).ConfigureAwait(false);
                        if (package.SendedMails is not null && options.HasFlag(WriterOptions.WriteSendedMails)) await WriteSendedMailsAsync(writer, package.SendedMails, token).ConfigureAwait(false);
                        if (package.Holidays is not null && options.HasFlag(WriterOptions.WriteHolidays)) await WriteHolidaysAsync(writer, package.Holidays, token).ConfigureAwait(false);
                        if (package.Cycles is not null && options.HasFlag(WriterOptions.WriteCycles)) await WriteCyclesAsync(writer, package.Cycles, token).ConfigureAwait(false);
                        if (package.Tactics is not null && options.HasFlag(WriterOptions.WriteTactics)) await WriteTacticsAsync(writer, package.Tactics, token).ConfigureAwait(false);
                        if (package.Stadiums is not null && options.HasFlag(WriterOptions.WriteStadiums)) await WriteStadiumsAsync(writer, package.Stadiums, token).ConfigureAwait(false);
                        if (package.Clubs is not null && options.HasFlag(WriterOptions.WriteClubs)) await WriteClubsAsync(writer, package.Clubs, token).ConfigureAwait(false);
                        if (package.Competitions is not null && options.HasFlag(WriterOptions.WriteCompetitions)) await WriteCompetitionsAsync(writer, package.Competitions, token).ConfigureAwait(false);
                    }
                }
                catch (IOException ex) when (ex.HResult == HResultNotEnoughFreeSpaceOnDiskError)
                {
                    throw new NotEnoughDiskSpaceException();
                }

                using (LogManager.MeasureTime($"Move file to : {filename}", TraceLevel.Debug))
                using (ProgressManager.StartUncancellable())
                {
                    // Remove old file
                    FileHelper.RemoveFile(filename);

                    MoveFileToFinalLocation(filename, tmpFile);
                }
            }

            return true;
        }


        private static async Task WritePlayersAsync(PackageWriter writer, PlayersPackage item, CancellationToken? token = null)
        {
            using var step = ProgressManager.Start();
            await writer.WriteStreamAsync(item, XmlConstants.PlayersUri, token).ConfigureAwait(false);

            var i = 0;
            var itemsWithPhoto = item.Where(x => x.Photo is not null).ToList();
            foreach (var p in itemsWithPhoto)
            {
                await writer.WriteImageAsync(p.Photo!, XmlConstants.PlayerPhotoUri.FormatWith(p.Id!), token).ConfigureAwait(false);
                i++;

                step?.UpdateProgress((double)i / itemsWithPhoto.Count);
            }
        }

        private static async Task WriteClubsAsync(PackageWriter writer, ClubsPackage item, CancellationToken? token = null)
        {
            using var step = ProgressManager.Start();
            await writer.WriteStreamAsync(item, XmlConstants.ClubsUri, token).ConfigureAwait(false);

            var i = 0;
            var itemsWithPhoto = item.Where(x => x.Logo is not null).ToList();
            foreach (var p in itemsWithPhoto)
            {
                await writer.WriteImageAsync(p.Logo!, XmlConstants.ClubLogoUri.FormatWith(p.Id!), token).ConfigureAwait(false);
                i++;

                step?.UpdateProgress((double)i / itemsWithPhoto.Count);
            }
        }

        private static async Task WriteCompetitionsAsync(PackageWriter writer, CompetitionsPackage item, CancellationToken? token = null)
        {
            using var step = ProgressManager.Start();
            await writer.WriteStreamAsync(item, XmlConstants.CompetitionsUri, token).ConfigureAwait(false);

            var i = 0;
            var itemsWithPhoto = item.Cups?.Concat(item.Leagues?.ToArray() ?? Array.Empty<CompetitionPackage>())
                                           .Concat(item.Friendlies?.ToArray() ?? Array.Empty<CompetitionPackage>())
                                           .Where(x => x.Logo is not null)
                                           .ToList() ?? [];
            foreach (var p in itemsWithPhoto)
            {
                await writer.WriteImageAsync(p.Logo!, XmlConstants.CompetitionLogoUri.FormatWith(p.Id!), token).ConfigureAwait(false);
                i++;

                step?.UpdateProgress((double)i / itemsWithPhoto.Count);
            }
        }

        private static async Task WriteSeasonsAsync(PackageWriter writer, SeasonsPackage item, CancellationToken? token = null)
        {
            using (ProgressManager.Start())
                await writer.WriteStreamAsync(item, XmlConstants.SeasonsUri, token).ConfigureAwait(false);
        }

        private static async Task WriteTrainingSessionsAsync(PackageWriter writer, TrainingSessionsPackage item, CancellationToken? token = null)
        {
            using (ProgressManager.Start())
                await writer.WriteStreamAsync(item, XmlConstants.TrainingSessionsUri, token).ConfigureAwait(false);
        }

        private static async Task WriteStadiumsAsync(PackageWriter writer, StadiumsPackage item, CancellationToken? token = null)
        {
            using (ProgressManager.Start())
                await writer.WriteStreamAsync(item, XmlConstants.StadiumsUri, token).ConfigureAwait(false);
        }

        private static async Task WriteSquadsAsync(PackageWriter writer, SquadsPackage item, CancellationToken? token = null)
        {
            using (ProgressManager.Start())
                await writer.WriteStreamAsync(item, XmlConstants.SquadsUri, token).ConfigureAwait(false);
        }

        private static async Task WriteTacticsAsync(PackageWriter writer, TacticsPackage item, CancellationToken? token = null)
        {
            using (ProgressManager.Start())
                await writer.WriteStreamAsync(item, XmlConstants.TacticsUri, token).ConfigureAwait(false);
        }

        private static async Task WriteSendedMailsAsync(PackageWriter writer, SendedMailsPackage item, CancellationToken? token = null)
        {
            using (ProgressManager.Start())
                await writer.WriteStreamAsync(item, XmlConstants.SendedMailsUri, token).ConfigureAwait(false);
        }

        private static async Task WriteHolidaysAsync(PackageWriter writer, HolidaysPackage item, CancellationToken? token = null)
        {
            using (ProgressManager.Start())
                await writer.WriteStreamAsync(item, XmlConstants.HolidaysUri, token).ConfigureAwait(false);
        }

        private static async Task WriteCyclesAsync(PackageWriter writer, CyclesPackage item, CancellationToken? token = null)
        {
            using (ProgressManager.Start())
                await writer.WriteStreamAsync(item, XmlConstants.CyclesUri, token).ConfigureAwait(false);
        }

        private static void MoveFileToFinalLocation(string filename, string temporaryFilename)
        {
            var temporaryFilenameInfo = new FileInfo(temporaryFilename);
            var fileInfo = new FileInfo(filename);

            if (string.IsNullOrEmpty(fileInfo.DirectoryName)) return;

            var driveinfo = new DriveInfo(fileInfo.DirectoryName);

            var diskInfo = driveinfo.HasEnoughSpace(temporaryFilenameInfo.Length);

            if (diskInfo == DiskDriveInfo.NoError)
                File.Move(temporaryFilename, filename);
            else
                throw new NotEnoughDiskSpaceException();
        }
    }
}
