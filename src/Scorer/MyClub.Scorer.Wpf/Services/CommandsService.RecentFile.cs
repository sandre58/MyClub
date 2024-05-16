// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MyClub.Scorer.Application.Contracts;
using MyNet.UI.Services;
using MyNet.Utilities.Logging;

namespace MyClub.Scorer.Wpf.Services
{
    internal class RecentFileCommandsService : IRecentFileCommandsService
    {
        private readonly IReadService _readService;
        private readonly ProjectCommandsService _projectCommandsService;

        public RecentFileCommandsService(ProjectCommandsService projectCommandsService, IReadService readService)
            => (_projectCommandsService, _readService) = (projectCommandsService, readService);

        public async Task<byte[]?> GetImageAsync(string file)
        {
            try
            {
                return await _readService.ReadImageAsync(file).ConfigureAwait(false);
            }
            catch (System.Exception e)
            {
                LogManager.Error(e);
                return null;
            }
        }

        public async Task OpenAsync(string file) => await _projectCommandsService.LoadAsync(file).ConfigureAwait(false);

        public async Task OpenCopyAsync(string file) => await _projectCommandsService.LoadTemplateAsync(file).ConfigureAwait(false);
    }
}
