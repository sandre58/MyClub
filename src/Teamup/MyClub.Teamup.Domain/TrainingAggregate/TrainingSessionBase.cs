// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;

namespace MyClub.Teamup.Domain.TrainingAggregate
{
    public class TrainingSessionBase : TrainingBase
    {
        private readonly ObservableCollection<ITrainingItem> _items = [];

        protected TrainingSessionBase(string theme, Guid? id = null) : base(theme, id) => Items = new(_items);

        public ReadOnlyObservableCollection<ITrainingItem> Items { get; }
    }
}
