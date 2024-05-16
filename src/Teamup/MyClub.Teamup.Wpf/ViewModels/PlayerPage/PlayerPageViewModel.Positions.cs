// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData;
using DynamicData.Binding;
using MyNet.UI.ViewModels.Workspace;
using MyClub.Domain.Enums;
using MyClub.Teamup.Wpf.ViewModels.Edition;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.PlayerPage
{
    internal class PlayerPagePositionsViewModel : SubItemViewModel<PlayerViewModel>
    {
        public IReadOnlyCollection<EditableRatedPositionViewModel> Positions { get; } = Position.GetPlayerPositions().Select(x => new EditableRatedPositionViewModel(x) { Rating = PositionRating.Inefficient }).ToArray();

        public EditableRatedPositionViewModel? SelectedPosition { get; set; }

        public PlayerPagePositionsViewModel()
            => Disposables.Add(ItemChanged.Subscribe(x =>
            {
                if (x is not null)
                {
                    ItemSubscriptions?.Add(
                        x.Positions.ToObservableChangeSet(y => y.Id)
                        .AutoRefresh(y => y.Rating)
                        .AutoRefresh(y => y.IsNatural)
                        .OnItemAdded(y =>
                        {
                            Positions.First(z => z.Position == y.Position).Rating = y.Rating;
                            Positions.First(z => z.Position == y.Position).IsNatural = y.IsNatural;
                        })
                        .OnItemRemoved(y =>
                        {
                            Positions.First(z => z.Position == y.Position).Rating = PositionRating.Inefficient;
                            Positions.First(z => z.Position == y.Position).IsNatural = false;
                        })
                        .OnItemRefreshed(y =>
                        {
                            Positions.First(z => z.Position == y.Position).Rating = y.Rating;
                            Positions.First(z => z.Position == y.Position).IsNatural = y.IsNatural;
                        })
                        .Subscribe());
                }
            }));
    }
}
