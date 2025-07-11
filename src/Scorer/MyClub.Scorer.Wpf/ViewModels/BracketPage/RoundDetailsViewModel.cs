// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.DynamicData.Extensions;
using MyNet.Observable;
using MyNet.UI.ViewModels.Workspace;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.BracketPage
{
    internal class RoundDetailsViewModel : ItemViewModel<RoundViewModel>
    {
        private readonly ExtendedObservableCollection<FixtureWrapper> _fixtures = [];

        public RoundDetailsViewModel()
        {
            Fixtures = new(_fixtures);
        }

        protected override void OnItemChanged()
        {
            base.OnItemChanged();

            if (Item is null) return;

            ItemSubscriptions?.AddRange([
                Item.Fixtures.ToObservableChangeSet().Transform(x => new FixtureWrapper(x)).Bind(_fixtures).Subscribe()
                ]);
        }

        public ReadOnlyObservableCollection<FixtureWrapper> Fixtures { get; private set; }
    }

    internal class FixtureWrapper : Wrapper<FixtureViewModel>
    {
        private readonly ExtendedObservableCollection<FixtureMatchWrapper> _matches = [];

        public FixtureWrapper(FixtureViewModel item) : base(item)
        {
            Matches = new(_matches);

            Disposables.AddRange([
                Item.Matches.ToObservableChangeSet().Transform(x => new FixtureMatchWrapper(x)).Bind(_matches).Subscribe()
                ]);
        }

        public ReadOnlyObservableCollection<FixtureMatchWrapper> Matches { get; private set; }
    }

    internal class FixtureMatchWrapper : Wrapper<MatchViewModel>
    {
        public FixtureMatchWrapper(MatchViewModel item) : base(item)
        {
        }
    }
}
