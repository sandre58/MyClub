// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyClub.CrossCutting.FileSystem;
using MyClub.CrossCutting.Packaging;
using MyClub.Scorer.Application.Contracts;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Infrastructure.Packaging.Converters;
using MyClub.Scorer.Infrastructure.Packaging.Models;
using MyNet.Utilities;
using MyNet.Utilities.Exceptions;
using MyNet.Utilities.Helpers;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Progress;

namespace MyClub.Scorer.Infrastructure.Packaging.Services
{
    public sealed class WriteService(ITempService tempService) : IWriteService
    {
        private const long HResultNotEnoughFreeSpaceOnDiskError = -2147024784;

        private readonly ITempService _tempService = tempService;

        public async Task<bool> WriteAsync(IProject project, string filename, CancellationToken? token = null)
            => await WriteAsync(project, filename, WriterOptions.WriteAll, token).ConfigureAwait(false);

        public async Task<bool> WriteAsync(IProject project, string filename, WriterOptions options, CancellationToken? token = null)
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

                        if (package.Stadiums is not null && options.HasFlag(WriterOptions.WriteStadiums)) await WriteStadiumsAsync(writer, package.Stadiums, token).ConfigureAwait(false);
                        if (package.Teams is not null && options.HasFlag(WriterOptions.WriteTeams)) await WriteTeamsAsync(writer, package.Teams, token).ConfigureAwait(false);
                        if (package.Competition is not null && options.HasFlag(WriterOptions.WriteAll)) await WriteCompetitionAsync(writer, package.Competition, token).ConfigureAwait(false);
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

        private static async Task WriteCompetitionAsync(PackageWriter writer, CompetitionPackage item, CancellationToken? token = null)
        {
            using (ProgressManager.Start())
                await writer.WriteStreamAsync(item, XmlConstants.CompetitionUri, token).ConfigureAwait(false);
        }

        private static async Task WriteStadiumsAsync(PackageWriter writer, StadiumsPackage item, CancellationToken? token = null)
        {
            using (ProgressManager.Start())
                await writer.WriteStreamAsync(item, XmlConstants.StadiumsUri, token).ConfigureAwait(false);
        }

        private static async Task WriteTeamsAsync(PackageWriter writer, TeamsPackage item, CancellationToken? token = null)
        {
            using var step = ProgressManager.Start();
            await writer.WriteStreamAsync(item, XmlConstants.TeamsUri, token).ConfigureAwait(false);

            var i = 0;
            var itemsWithPhoto = item.Where(x => x.Logo is not null).ToList();
            var playersWithPhoto = item.SelectMany(x => x.Players ?? []).Where(x => x.Photo is not null).ToList();
            var managersWithPhoto = item.SelectMany(x => x.Staff ?? []).Where(x => x.Photo is not null).ToList();
            var count = itemsWithPhoto.Count + playersWithPhoto.Count + managersWithPhoto.Count;
            await writeImageAsync(itemsWithPhoto.ToDictionary(x => x.Id, x => x.Logo!), XmlConstants.TeamLogoUri).ConfigureAwait(false);
            await writeImageAsync(playersWithPhoto.ToDictionary(x => x.Id, x => x.Photo!), XmlConstants.PlayerPhotoUri).ConfigureAwait(false);
            await writeImageAsync(managersWithPhoto.ToDictionary(x => x.Id, x => x.Photo!), XmlConstants.StaffPhotoUri).ConfigureAwait(false);

            async Task writeImageAsync(IDictionary<Guid, byte[]> images, string uri)
            {
                foreach (var image in images)
                {
                    await writer.WriteImageAsync(image.Value, uri.FormatWith(image.Key), token).ConfigureAwait(false);
                    i++;

                    step?.UpdateProgress((double)i / count);
                }
            }
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
