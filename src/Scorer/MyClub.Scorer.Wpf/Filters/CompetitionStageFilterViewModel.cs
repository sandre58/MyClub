// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.UI.Commands;
using MyNet.Utilities;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using System.Collections.ObjectModel;
using DynamicData.Binding;
using DynamicData;

namespace MyClub.Scorer.Wpf.Filters
{
    internal class CompetitionStageFilterViewModel : SelectedValueFilterViewModel<ICompetitionStageViewModel, ICompetitionStageViewModel>
    {
        public CompetitionStageFilterViewModel(string propertyName, IEnumerable<ICompetitionStageViewModel> allowedValues) : base(propertyName, allowedValues)
        {
            PreviousStageCommand = CommandsManager.Create(() => Value = GetPreviousStage(Value), () => GetPreviousStage(Value) is not null);
            NextStageCommand = CommandsManager.Create(() => Value = GetNextStage(Value), () => GetNextStage(Value) is not null);
            NextFixturesCommand = CommandsManager.Create(() => Value = GetNextFixtures(), () => GetNextFixtures() is ICompetitionStageViewModel matchStage && matchStage != Value);
            LatestResultsCommand = CommandsManager.Create(() => Value = GetLatestResults(), () => GetLatestResults() is ICompetitionStageViewModel matchStage && matchStage != Value);

            if (allowedValues is ReadOnlyObservableCollection<ICompetitionStageViewModel> collection)
            {
                collection.ToObservableChangeSet().OnItemAdded(x => Value.IfNull(Reset))
                                                  .OnItemRemoved(x => (Value == x).IfTrue(Reset))
                                                  .Subscribe();
            }
        }

        public ICommand PreviousStageCommand { get; private set; }

        public ICommand NextStageCommand { get; private set; }

        public ICommand NextFixturesCommand { get; private set; }

        public ICommand LatestResultsCommand { get; private set; }

        private ICompetitionStageViewModel? GetPreviousStage(ICompetitionStageViewModel? stage)
            => AvailableValues?.Except([stage]).OrderBy(x => x!.StartDate).LastOrDefault(x => stage is null || x!.StartDate.IsBefore(stage.StartDate.Add(1.Milliseconds())));

        private ICompetitionStageViewModel? GetNextStage(ICompetitionStageViewModel? stage)
            => AvailableValues?.Except([stage]).OrderBy(x => x!.StartDate).FirstOrDefault(x => stage is null || x!.StartDate.IsAfter(stage.StartDate.Subtract(1.Milliseconds())));

        private ICompetitionStageViewModel? GetNextFixtures() => AvailableValues?.OrderBy(x => x.StartDate).FirstOrDefault(x => x.StartDate.IsAfter(DateTime.Now));

        private ICompetitionStageViewModel? GetLatestResults() => AvailableValues?.OrderBy(x => x.StartDate).LastOrDefault(x => x.StartDate.IsBefore(DateTime.Now));

        public override void Reset() => Value = GetNextFixtures() ?? GetLatestResults();

        protected override FilterViewModel CreateCloneInstance() => new CompetitionStageFilterViewModel(PropertyName, AvailableValues ?? []);
    }
}
