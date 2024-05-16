// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using DynamicData;
using MyNet.Utilities.Messaging;
using MyClub.Scorer.Wpf.Services.Providers.Base;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Application.Messages;
using MyClub.Scorer.Domain.ProjectAggregate;

namespace MyClub.Scorer.Wpf.Services.Providers
{
    internal class MatchesProvider : EntitiesProviderBase<MatchViewModel>
    {
        private readonly CompetitionInfoProvider _competitionInfoProvider;

        public MatchesProvider(ProjectInfoProvider projectInfoProvider, CompetitionInfoProvider competitionInfoProvider) : base(projectInfoProvider)
        {
            _competitionInfoProvider = competitionInfoProvider;

            Messenger.Default.Register<MatchConflictsValidationMessage>(this, OnMatchConflictsValidation);
        }

        protected override IObservable<IChangeSet<MatchViewModel, Guid>> ProvideObservable(IProject project) => _competitionInfoProvider.GetCompetition().ProvideMatches();

        private void OnMatchConflictsValidation(MatchConflictsValidationMessage message)
        {
            foreach (var match in Items)
            {
                var conflicts = message.Conflicts.Where(x => x.Item2 == match.Id).Select(x => new MatchConflict(x.Item1, x.Item3.HasValue ? Get(x.Item3.Value) : null))
                    .Union(message.Conflicts.Where(x => x.Item3 == match.Id).Select(x => new MatchConflict(x.Item1, Get(x.Item2)))).ToList();

                match.SetConflicts(conflicts);
            }
        }
    }
}
