// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using MyNet.Utilities;

namespace MyClub.Domain
{
    public interface IEntity : IIdentifiable<Guid>, IComparable, INotifyPropertyChanged
    {
    }
}
