// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.Utilities;
using PropertyChanged;

namespace MyClub.Domain
{
    [AddINotifyPropertyChangedInterface]
    public abstract class Contact : ValueObject
    {
        private string _value = null!;

        protected Contact(string value, string? label = null, bool isDefault = false)
        {
            Value = value;
            Label = label;
            Default = isDefault;
        }

        public string? Label { get; set; }

        public bool Default { get; set; }

        public string Value
        {
            get => _value;
            set => _value = ValidateOrThrowValue(value);
        }

        protected abstract string ValidateOrThrowValue(string value);

        public override string ToString() => Value;
    }
}
