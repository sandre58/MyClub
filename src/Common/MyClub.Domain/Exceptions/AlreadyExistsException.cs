// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.Utilities.Exceptions;

namespace MyClub.Domain.Exceptions
{
    public class AlreadyExistsException : TranslatableException
    {
        public AlreadyExistsException(string propertyName, object value)
            : base($"'{value}' already exists in '{propertyName}'.", "FieldXMustHaveUniqueItemsError", propertyName) { }
    }
}
