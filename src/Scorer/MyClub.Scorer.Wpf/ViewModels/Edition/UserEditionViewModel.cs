// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using MyClub.CrossCutting.Localization;
using MyClub.UserContext.Application.Dtos;
using MyClub.UserContext.Application.Services;
using MyClub.UserContext.Domain.UserAggregate;
using MyNet.Observable.Attributes;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Edition;

namespace MyClub.Scorer.Wpf.ViewModels.Edition
{
    public class UserEditionViewModel : EditionViewModel
    {
        private readonly IUserRepository _userRepository;
        private readonly UserService _userService;

        [Display(Name = nameof(DisplayName), ResourceType = typeof(MyClubResources))]
        public string? DisplayName { get; set; }

        [IsEmailAddress(true)]
        [Display(Name = nameof(Email), ResourceType = typeof(MyClubResources))]
        public string? Email { get; set; }

        [Display(Name = nameof(Image), ResourceType = typeof(MyClubResources))]
        public byte[]? Image { get; set; }

        public UserEditionViewModel(IUserRepository userRepository, UserService userService)
        {
            _userRepository = userRepository;
            _userService = userService;
            Mode = ScreenMode.Edition;
        }

        protected override string CreateTitle() => MyClubResources.EditProfile;

        protected override void RefreshCore()
        {
            var user = _userRepository.GetCurrent();
            DisplayName = user.DisplayName;
            Email = user.Email;
            Image = user.Image;
        }

        protected override void SaveCore() => _userService.Save(new UserDto
        {
            DisplayName = DisplayName,
            Email = Email,
            Image = Image
        });
    }
}
