// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Linq;
using GongSolutions.Wpf.DragDrop;
using MyNet.Utilities;
using MyClub.Domain;

namespace MyClub.Teamup.Wpf.DragAndDrop
{
    public class OrderDropHandler<T> : DefaultDropHandler where T : IOrderable
    {
        public override void Drop(IDropInfo dropInfo)
        {
            base.Drop(dropInfo);

            dropInfo.TargetCollection.OfType<T>().Select((x, index) => (Index: index, Value: x)).ForEach(x => x.Value.Order = x.Index + 1);
        }
    }
}
