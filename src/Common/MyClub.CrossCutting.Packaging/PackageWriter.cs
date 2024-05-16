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
    public sealed class PackageWriter(string filename) : IDisposable
    {
        private readonly Package _package = Package.Open(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

        public async Task WriteImageAsync(byte[] thumbnail, string uriPath, CancellationToken? token = null)
        {
            using var stream = CreatePart(uriPath, XmlConstants.ContentTypePng).GetStream(FileMode.Create);
            await stream.WriteImageAsync(thumbnail, token).ConfigureAwait(false);
        }

        public async Task WriteAsync<T>(T item, string uri, CancellationToken? token)
        {
            using (ProgressManager.Start())
                await WriteStreamAsync(item, uri, token).ConfigureAwait(false);
        }

        public async Task WriteStreamAsync<T>(T item, string uri, CancellationToken? token)
        {
            using (LogManager.MeasureTime($"Write {uri}", TraceLevel.Debug))
            {
                using var stream = CreatePart(uri).GetStream(FileMode.Create);
                await stream.WriteAsXmlAsync(item, token).ConfigureAwait(false);
            }
        }

        private PackagePart CreatePart(string uriPath, string contentType = XmlConstants.ContentTypeXml)
        {
            var uri = PackUriHelper.CreatePartUri(new Uri(uriPath, UriKind.RelativeOrAbsolute));
            return _package.CreatePart(uri, contentType, CompressionOption.Normal);
        }

        public void Dispose() => _package.Close();
    }
}
