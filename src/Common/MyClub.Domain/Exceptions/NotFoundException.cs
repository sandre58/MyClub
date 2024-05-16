// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.Utilities.Exceptions;

namespace MyClub.Domain.Exceptions
{
    public class NotFoundException : TranslatableException
    {
        public NotFoundException(object item)
            : base($"Item '{item}' not found.", "ItemXNotFoundException", item) { }
    }
}
