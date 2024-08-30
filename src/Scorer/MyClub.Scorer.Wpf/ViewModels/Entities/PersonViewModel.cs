// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Domain.PersonAggregate;
using MyNet.UI.Commands;
using MyNet.Utilities;
using MyNet.Utilities.Geography;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal abstract class PersonViewModel : EntityViewModelBase<Person>
    {
        protected PersonViewModel(Person item) : base(item)
        {
            OpenCommand = CommandsManager.Create(async () => await OpenAsync().ConfigureAwait(false));
            EditCommand = CommandsManager.Create(async () => await EditAsync().ConfigureAwait(false));

            Disposables.AddRange(
            [
                Item.WhenAnyPropertyChanged(nameof(Player.LastName), nameof(Player.FirstName)).Subscribe(_ =>
                {
                    RaisePropertyChanged(nameof(InverseName));
                    RaisePropertyChanged(nameof(FullName));
                    RaisePropertyChanged(nameof(FirstLetter));
                }),
            ]);
        }

        public string LastName => Item.LastName;

        public string FirstName => Item.FirstName;

        public Country? Country => Item.Country;

        public byte[]? Photo => Item.Photo;

        public GenderType Gender => Item.Gender;

        public string? LicenseNumber => Item.LicenseNumber;

        public string InverseName => Item.GetInverseName();

        public string FullName => Item.GetFullName();

        public string FirstLetter => Item.LastName.Substring(0, 1).ToUpperInvariant();

        public ICommand EditCommand { get; }

        public ICommand OpenCommand { get; }

        public abstract Task OpenAsync();

        public abstract Task EditAsync();
    }
}
