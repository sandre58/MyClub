// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;
using MyNet.Observable.Attributes;
using MyNet.UI.Collections;
using MyNet.UI.ViewModels.Workspace;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    internal class EditableMatchRulesViewModel : NavigableWorkspaceViewModel
    {
        public EditableMatchRulesViewModel() => Reset();

        public UiObservableCollection<CardColor> Cards { get; } = [];

        [CanBeValidated(false)]
        [CanSetIsModified(false)]
        public UiObservableCollection<CardColor> UnusedCards { get; } = [];


        public MatchRules Create() => new(Cards);

        public void Load(MatchRules matchRules)
        {
            Cards.Set(matchRules.AllowedCards);
            UnusedCards.Set(Enum.GetValues<CardColor>().Except(Cards));
        }

        protected override void ResetCore() => Load(MatchRules.Default);
    }
}
