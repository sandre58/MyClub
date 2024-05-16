// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.IO.Packaging;
using System.Threading;
using System.Threading.Tasks;
using MyNet.Utilities.Extensions;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Progress;

namespace MyClub.CrossCutting.Packaging
{
    public sealed class PackageReader(string filename) : IDisposable
    {
        private readonly Package _package = Package.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None);

        public async Task<byte[]?> ReadImageAsync(string uriPath, CancellationToken? token = null)
        {
            try
            {
                using var stream = FindPart(uriPath).GetStream(FileMode.Open);
                return await stream.ReadImageAsync(token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                LogManager.Info($"No image found in {uriPath}");
                return null;
            }
        }

        public async Task<TModel?> ReadAsync<TModel>(string uri, CancellationToken? token)
        {
            using (ProgressManager.Start())
                return await ReadStreamAsync<TModel>(uri, token).ConfigureAwait(false);
        }

        private PackagePart FindPart(string uriPath)
        {
            var uri = PackUriHelper.CreatePartUri(new Uri(uriPath, UriKind.RelativeOrAbsolute));
            return _package.GetPart(uri);
        }

        public async Task<TModel?> ReadStreamAsync<TModel>(string uri, CancellationToken? token)
        {
            using (LogManager.MeasureTime($"Read {uri}", TraceLevel.Debug))
            {
                using var stream = FindPart(uri).GetStream(FileMode.Open);
                return await stream.ReadAsXmlAsync<TModel>(token).ConfigureAwait(false);
            }
        }

        public void Dispose() => _package.Close();
    }
}
