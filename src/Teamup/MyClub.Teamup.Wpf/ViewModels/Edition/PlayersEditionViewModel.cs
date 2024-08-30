// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using MyClub.CrossCutting.Localization;
using MyClub.Domain.Enums;
using MyClub.Teamup.Application.Dtos;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Domain.Enums;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.Humanizer;
using MyNet.Observable.Attributes;
using MyNet.UI.Commands;
using MyNet.UI.Resources;
using MyNet.UI.Toasting;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Edition;
using MyNet.Utilities;
using MyNet.Utilities.Geography;
using MyNet.Wpf.Presentation.Models;

namespace MyClub.Teamup.Wpf.ViewModels.Edition
{
    internal class PlayersEditionViewModel : EditionViewModel
    {
        private readonly PlayerService _playerService;

        [CanSetIsModified(false)]
        public IReadOnlyCollection<PlayerViewModel> Players { get; private set; } = new List<PlayerViewModel>().AsReadOnly();

        public MultipleEditableValue<Category?> Category { get; set; } = new();

        [Display(Name = nameof(Country), ResourceType = typeof(MyClubResources))]
        public MultipleEditableValue<Country?> Country { get; set; } = new();

        [Display(Name = nameof(LicenseState), ResourceType = typeof(MyClubResources))]
        public MultipleEditableValue<LicenseState> LicenseState { get; set; } = new();

        [Display(Name = nameof(IsMutation), ResourceType = typeof(MyClubResources))]
        public MultipleEditableValue<bool> IsMutation { get; set; } = new();

        [Display(Name = nameof(ShoesSize), ResourceType = typeof(MyClubResources))]
        public MultipleEditableValue<int?> ShoesSize { get; } = new();

        [Display(Name = nameof(Size), ResourceType = typeof(MyClubResources))]
        public MultipleEditableValue<string?> Size { get; set; } = new();

        [CanSetIsModified(false)]
        [CanBeValidated(false)]
        [UpdateOnCultureChanged]
        public virtual IList<string> Sizes => MyClubResources.SizesList.Split(';');

        public ICommand UpSizeCommand { get; }

        public ICommand DownSizeCommand { get; }

        public PlayersEditionViewModel(PlayerService playerService)
        {
            _playerService = playerService;
            Mode = ScreenMode.Edition;

            DownSizeCommand = CommandsManager.Create<object>(DownSize, CanDownSize);
            UpSizeCommand = CommandsManager.Create<object>(UpSize, CanUpSize);
        }

        public void Load(IEnumerable<PlayerViewModel> players)
        {
            Players = players.ToList().AsReadOnly();
            Reset();
        }

        protected override void ResetCore()
        {
            base.ResetCore();

            Country.Reset(Players.Select(x => x.Country));
            Category.Reset(Players.Select(x => x.Category));
            LicenseState.Reset(Players.Select(x => x.LicenseState));
            IsMutation.Reset(Players.Select(x => x.IsMutation));
            ShoesSize.Reset(Players.Select(x => x.ShoesSize));
            Size.Reset(Players.Select(x => x.Size));
        }

        #region Validate

        protected override void SaveCore()
        {
            if (!Category.IsActive && !IsMutation.IsActive && !LicenseState.IsActive && !ShoesSize.IsActive && !Size.IsActive && !Country.IsActive) return;

            var result = _playerService.Update(Players.Select(x => x.Id).ToList(), new PlayerMultipleDto
            {
                UpdateCategory = Category.IsActive,
                UpdateIsMutation = IsMutation.IsActive,
                UpdateLicenseState = LicenseState.IsActive,
                UpdateShoesSize = ShoesSize.IsActive,
                UpdateSize = Size.IsActive,
                UpdateCountry = Country.IsActive,
                Category = Category.GetActiveValue(),
                IsMutation = IsMutation.GetActiveValue(),
                Country = Country.GetActiveValue(),
                ShoesSize = ShoesSize.GetActiveValue(),
                Size = Size.GetActiveValue(),
                LicenseState = LicenseState.GetActiveValue(),
            });

            ToasterManager.ShowSuccess(nameof(MessageResources.XItemsHasBeenModifiedSuccess).TranslateAndFormatWithCount(result.Count));
        }

        #endregion

        #region Sizes

        private void DownSize(object? obj) => Size.Value = Sizes.IndexOf(Size.Value.OrEmpty()) > 0 ? Sizes[Sizes.IndexOf(Size.Value.OrEmpty()) - 1] : Sizes.LastOrDefault();

        private bool CanDownSize(object? obj) => Sizes.IndexOf(Size.Value.OrEmpty()) > 0 || Sizes.IndexOf(Size.Value.OrEmpty()) == -1;

        private void UpSize(object? obj) => Size.Value = Sizes.IndexOf(Size.Value.OrEmpty()) < Sizes.Count - 1 && Sizes.IndexOf(Size.Value.OrEmpty()) != -1
                ? Sizes[Sizes.IndexOf(Size.Value.OrEmpty()) + 1]
                : Sizes.FirstOrDefault();

        private bool CanUpSize(object? obj) => Sizes.IndexOf(Size.Value.OrEmpty()) < Sizes.Count - 1 || Sizes.IndexOf(Size.Value.OrEmpty()) == -1;

        #endregion
    }
}
