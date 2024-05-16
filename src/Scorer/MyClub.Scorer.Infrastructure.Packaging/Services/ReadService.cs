// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MyClub.CrossCutting.Packaging;
using MyClub.Scorer.Application.Contracts;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.StadiumAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyClub.Scorer.Infrastructure.Packaging.Converters;
using MyClub.Scorer.Infrastructure.Packaging.Models;
using MyNet.Utilities;
using MyNet.Utilities.IO.FileExtensions;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Progress;

namespace MyClub.Scorer.Infrastructure.Packaging.Services
{
    public sealed class ReadService : IReadService
    {
        public bool CanRead(string filename) => ScprojFileExtensionInfo.Scproj.IsValid(filename);

        public async Task<IProject> ReadAsync(string filename, CancellationToken? token = null)
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

        public async Task<IEnumerable<Stadium>> ReadStadiumsAsync(string filename, CancellationToken? token = null)
        {
            var package = await ReadAsync(filename, ReaderOptions.ReadStadiums, token).ConfigureAwait(false);

            var stadiums = package.Stadiums?.Select(x => x.CreateStadium()).ToArray() ?? [];

            return stadiums ?? [];
        }

        public async Task<IEnumerable<Team>> ReadTeamsAsync(string filename, CancellationToken? token = null)
        {
            var package = await ReadAsync(filename, ReaderOptions.ReadMetadata | ReaderOptions.ReadStadiums | ReaderOptions.ReadTeams, token).ConfigureAwait(false);

            var stadiums = package.Stadiums?.Select(x => x.CreateStadium()).ToArray() ?? [];
            var teams = package.Teams?.Select(x => x.CreateTeam(x.StadiumId.HasValue ? stadiums.GetByIdOrDefault(x.StadiumId.Value) : null)).ToArray() ?? [];

            return teams;
        }

        private static async Task<ProjectPackage> ReadAsync(string filename, ReaderOptions options, CancellationToken? token = null)
        {
            using (ProgressManager.Start(options.CountFlags()))
            {
                using var reader = new PackageReader(filename);

                var projectModel = new ProjectPackage();

                if (options.HasFlag(ReaderOptions.ReadMetadata)) projectModel.Metadata = await ReadMetadataAsync(reader, token).ConfigureAwait(false);
                token?.ThrowIfCancellationRequested();
                if (options.HasFlag(ReaderOptions.ReadStadiums)) projectModel.Stadiums = await ReadStadiumsAsync(reader, token).ConfigureAwait(false);
                token?.ThrowIfCancellationRequested();
                if (options.HasFlag(ReaderOptions.ReadTeams)) projectModel.Teams = await ReadTeamsAsync(reader, token).ConfigureAwait(false);
                token?.ThrowIfCancellationRequested();
                if (options.HasFlag(ReaderOptions.ReadAll)) projectModel.Competition = await ReadCompetitionAsync(reader, token).ConfigureAwait(false);
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

        private static async Task<StadiumsPackage?> ReadStadiumsAsync(PackageReader reader, CancellationToken? token = null)
        {
            using (ProgressManager.Start())
                return await reader.ReadStreamAsync<StadiumsPackage>(XmlConstants.StadiumsUri, token).ConfigureAwait(false);
        }

        private static async Task<CompetitionPackage?> ReadCompetitionAsync(PackageReader reader, CancellationToken? token = null)
        {
            using (ProgressManager.Start())
                return await reader.ReadStreamAsync<CompetitionPackage>(XmlConstants.CompetitionUri, token).ConfigureAwait(false);
        }

        private static async Task<TeamsPackage?> ReadTeamsAsync(PackageReader reader, CancellationToken? token = null)
        {
            using var step = ProgressManager.Start();
            var result = await reader.ReadStreamAsync<TeamsPackage>(XmlConstants.TeamsUri, token).ConfigureAwait(false);

            var i = 0;
            if (result is not null)
            {
                foreach (var p in result)
                {
                    token?.ThrowIfCancellationRequested();
                    p.Logo = await reader.ReadImageAsync(XmlConstants.TeamLogoUri.FormatWith(p.Id), token).ConfigureAwait(false);

                    foreach (var item in p.Players ?? [])
                    {
                        token?.ThrowIfCancellationRequested();
                        item.Photo = await reader.ReadImageAsync(XmlConstants.PlayerPhotoUri.FormatWith(item.Id), token).ConfigureAwait(false);
                    }

                    foreach (var item in p.Staff ?? [])
                    {
                        token?.ThrowIfCancellationRequested();
                        item.Photo = await reader.ReadImageAsync(XmlConstants.StaffPhotoUri.FormatWith(item.Id), token).ConfigureAwait(false);
                    }

                    i++;

                    step?.UpdateProgress((double)i / result.Count);
                }
            }

            return result;
        }
    }
}
