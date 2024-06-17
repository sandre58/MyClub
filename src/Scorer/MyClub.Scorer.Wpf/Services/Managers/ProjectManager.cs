// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Wpf.Messages;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.UI.Services;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Messaging;
using MyNet.Utilities.Threading;

namespace MyClub.Scorer.Wpf.Services.Managers
{
    internal sealed class ProjectManager : IDisposable
    {
        private readonly AvailibilityCheckingService _availibilityCheckingService;
        private readonly MatchesProvider _matchesProvider;
        private readonly SingleTaskRunner _checkConflictsRunner;

        public ProjectManager(MatchesProvider matchesProvider,
                              AvailibilityCheckingService availibilityCheckingService)
        {
            _availibilityCheckingService = availibilityCheckingService;
            _matchesProvider = matchesProvider;
            _checkConflictsRunner = new SingleTaskRunner(async x => await CheckConflictsAsync(x).ConfigureAwait(false));

            Messenger.Default.Register<CheckConflictsRequestMessage>(this, _ => _checkConflictsRunner.Run());
        }

        private async Task CheckConflictsAsync(CancellationToken cancellationToken)
        {
            LogManager.Debug("Check conflicts");

            await AppBusyManager.BackgroundAsync(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var conflicts = _availibilityCheckingService.GetAllConflicts();
                var convertedConflicts = conflicts.Select(x => (x.Item1, _matchesProvider.GetOrThrow(x.Item2), x.Item3.HasValue ? _matchesProvider.Get(x.Item3.Value) : null)).ToList();

                foreach (var match in _matchesProvider.Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var matchConflicts = convertedConflicts.Where(x => match.Id == x.Item2.Id).Select(x => new MatchConflict(x.Item1, x.Item3))
                        .Union(convertedConflicts.Where(x => match.Id == x.Item3?.Id).Select(x => new MatchConflict(x.Item1, x.Item2))).ToList();

                    match.SetConflicts(matchConflicts);
                }

                cancellationToken.ThrowIfCancellationRequested();

                MyNet.UI.Threading.Scheduler.GetUIOrCurrent().Schedule(() => Messenger.Default.Send(new MatchConflictsValidationMessage(convertedConflicts)));

            }).ConfigureAwait(false);
        }
        public void Dispose() => _checkConflictsRunner.Dispose();
    }
}
