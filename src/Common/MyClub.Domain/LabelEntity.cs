// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyNet.Utilities;
using MyNet.Utilities.Extensions;

namespace MyClub.Domain
{
    public class LabelEntity : AuditableEntity, IOrderable, ISimilar
    {
        private string _label = string.Empty;

        protected LabelEntity(string label, Guid? id = null) : base(id) => Label = label;

        public string Code { get; set; } = string.Empty;

        public string Label
        {
            get => _label;
            set => _label = value.IsRequiredOrThrow();
        }

        public string? Description { get; set; }

        public int Order { get; set; }

        public override int CompareTo(object? obj)
        {
            if (obj is LabelEntity other)
            {
                var value = Order.CompareTo(other.Order);

                return value != 0 ? value : string.Compare(Label, other.Label, StringComparison.OrdinalIgnoreCase);
            }
            return 1;
        }

        public virtual bool IsSimilar(object? obj) => obj is LabelEntity other && (Code.Equals(other.Code) || Label.Equals(other.Label));

        public override string ToString() => Label;
    }
}
