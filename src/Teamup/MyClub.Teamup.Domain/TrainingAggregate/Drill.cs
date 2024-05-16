// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using MyClub.Teamup.Domain.Enums;
using MyClub.Domain.Training.TrainingAggregate.DrawingObjects;

namespace MyClub.Teamup.Domain.TrainingAggregate
{
    public class Drill : TrainingBase, ITrainingItem
    {
        private readonly ObservableCollection<DrawingObject> _drawingObjects = [];

        public Drill(string theme, Guid? id = null) : base(theme, id) => DrawingObjects = new(_drawingObjects);

        string ITrainingItem.Label => Theme;

        public TimeSpan? Duration { get; set; }

        public DrillType Type { get; set; } = DrillType.Exercice;

        public Area? Area { get; set; }

        public ReadOnlyObservableCollection<DrawingObject> DrawingObjects { get; }

        public ObservableCollection<string> Instructions { get; } = [];

        public ObservableCollection<string> Variations { get; } = [];

        public byte[]? Schema { get; set; }
    }
}
