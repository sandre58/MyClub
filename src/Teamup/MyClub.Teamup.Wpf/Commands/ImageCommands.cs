// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Windows.Media.Imaging;
using MyNet.UI.Commands;
using MyNet.UI.Dialogs;
using MyNet.UI.Dialogs.Settings;
using MyNet.UI.Extensions;
using MyNet.UI.Messages;
using MyNet.UI.Services;
using MyNet.Utilities;
using MyNet.Utilities.Helpers;
using MyNet.Utilities.IO.FileExtensions;
using MyNet.Utilities.Messaging;

namespace MyClub.Teamup.Wpf.Commands
{
    internal static class ImageCommands
    {
        public static ICommand DownloadCommand { get; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3963:\"static\" fields should be initialized inline", Justification = "CommandsManager must be initialized")]
        static ImageCommands() => DownloadCommand = CommandsManager.Create<BitmapSource>(async x =>
        {
            if (x is not BitmapImage bitmap) return;

            var (result, filename) = await DialogManager.ShowSaveFileDialogAsync(new SaveFileDialogSettings
            {
                Filters = FileExtensionFilterBuilderProvider.AllImages.GenerateFilters(),
                DefaultExtension = FileExtensionInfoProvider.Jpg.GetDefaultExtension(),
                CheckFileExists = false
            }).ConfigureAwait(false);

            if (result.IsTrue())
                await AppBusyManager.BackgroundBusyService.WaitIndeterminateAsync(() =>
                {
                    var fullPath = Path.GetFullPath(filename);

                    var encoder = CreateBitmapEncoder(fullPath);
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));

                    using var stream = File.Create(fullPath);
                    encoder.Save(stream);

                    Messenger.Default.Send(new FileExportedMessage(fullPath, ProcessHelper.Start));
                }).ConfigureAwait(false);
        }, x => x is BitmapImage);

        private static BitmapEncoder CreateBitmapEncoder(string filePath) => Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".bmp" => new BmpBitmapEncoder(),
            ".gif" => new GifBitmapEncoder(),
            ".jpeg" or ".jpg" or ".jpe" => new JpegBitmapEncoder(),
            ".png" => new PngBitmapEncoder(),
            ".tiff" or ".tif" => new TiffBitmapEncoder(),
            ".wdp" or ".hdp" => new WmpBitmapEncoder(),
            _ => throw new ArgumentException("Can not encode bitmaps for the specified file extension.", nameof(filePath)),
        };
    }
}
