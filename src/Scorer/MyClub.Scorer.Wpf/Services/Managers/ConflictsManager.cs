// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Wpf.Messages;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.Observable.Deferrers;
using MyNet.UI.Services;
using MyNet.Utilities;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Messaging;

namespace MyClub.Scorer.Wpf.Services.Managers
{
    internal sealed class ConflictsManager : IDisposable
    {
        private readonly MatchesProvider _matchesProvider;
        private readonly AvailibilityCheckingService _availibilityCheckingService;
        private readonly SingleTaskDeferrer _checkConflictsDeferrer;
        private CompositeDisposable _matchesDisposables = [];
        private CompositeDisposable _competitionDisposables = [];

        public ConflictsManager(CompetitionInfoProvider competitionInfoProvider, MatchesProvider matchesProvider, AvailibilityCheckingService availibilityCheckingService)
        {
            _matchesProvider = matchesProvider;
            _availibilityCheckingService = availibilityCheckingService;

            _checkConflictsDeferrer = new(async x => await CheckConflictsAsync(x).ConfigureAwait(false), throttle: 500);

            matchesProvider.LoadRunner.RegisterOnEnd(this, _ => _matchesDisposables = new([
                    matchesProvider.Connect().WhereReasonsAre(ListChangeReason.Add, ListChangeReason.Remove, ListChangeReason.RemoveRange, ListChangeReason.AddRange, ListChangeReason.Clear).Subscribe(_ => _checkConflictsDeferrer.AskRefresh()),
                    matchesProvider.Connect().WhenAnyPropertyChanged([nameof(MatchViewModel.Date), nameof(MatchViewModel.State), nameof(MatchViewModel.Stadium)]).Subscribe(_ => _checkConflictsDeferrer.AskRefresh()),
                    competitionInfoProvider.UnloadRunner.WhenStart().Subscribe(_ => _competitionDisposables.Dispose()),
                    competitionInfoProvider.LoadRunner.WhenEnd().Subscribe(x =>
                    {
                        if (x is null) return;
                        _competitionDisposables = new([
                            x.WhenPropertyChanged(x => x.MatchFormat, false).Subscribe(_ => _checkConflictsDeferrer.AskRefresh()),
                            x.SchedulingParameters.WhenAnyPropertyChanged().Subscribe(_ => _checkConflictsDeferrer.AskRefresh()),
                        ]);
                    })
                ]));

            matchesProvider.UnloadRunner.RegisterOnEnd(this, () => _matchesDisposables.Dispose());
        }

        public IDisposable Defer() => _checkConflictsDeferrer.Defer();

        private async Task CheckConflictsAsync(CancellationToken cancellationToken)
            => await AppBusyManager.BackgroundAsync(() =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using (LogManager.MeasureTime("Check conflicts"))
                    {
                        var conflicts = _availibilityCheckingService.GetAllConflicts();
                        var matches = _matchesProvider.Items.ToList();
                        var convertedConflicts = conflicts.Select(x => (x.Item1, matches.GetById(x.Item2), x.Item3.HasValue ? matches.GetById(x.Item3.Value) : null)).ToList();

                        foreach (var match in matches)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var matchConflicts = convertedConflicts.Where(x => match.Id == x.Item2.Id).Select(x => new MatchConflict(x.Item1, x.Item3))
                                .Union(convertedConflicts.Where(x => match.Id == x.Item3?.Id).Select(x => new MatchConflict(x.Item1, x.Item2))).ToList();

                            match.SetConflicts(matchConflicts);
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        Messenger.Default.Send(new MatchConflictsValidationMessage(convertedConflicts));
                    }
                }
                catch (Exception)
                {
                    // Nothing
                }
            }).ConfigureAwait(false);

        public void Dispose()
        {
            _matchesProvider.LoadRunner.Unregister(this);
            _matchesProvider.UnloadRunner.Unregister(this);
            _matchesDisposables.Dispose();
            _competitionDisposables.Dispose();
            _checkConflictsDeferrer.Dispose();
        }
    }
}
