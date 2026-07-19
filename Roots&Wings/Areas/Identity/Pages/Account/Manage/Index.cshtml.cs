// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Roots_Wings.Models;
using Roots_Wings.Services.Interfaces;

namespace Roots_Wings.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IProfilePictureService _profilePictureService;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IProfilePictureService profilePictureService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _profilePictureService = profilePictureService;
        }

        public string Username { get; set; }
        public string CurrentPicture { get; set; }
        public bool HasPicture { get; set; }   // false when on the default - Remove disabled

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        // For the instant picture upload (separate from the name form)
        [BindProperty]
        public IFormFile PictureUpload { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(30, MinimumLength = 3, ErrorMessage = "The Name must be between 3 and 30 characters long.")]
            [Display(Name = "Name")]
            public string Name { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            Username = await _userManager.GetUserNameAsync(user);
            CurrentPicture = user.ProfilePictureOrDefault;
            HasPicture = !string.IsNullOrWhiteSpace(user.ProfilePicture);

            Input = new InputModel { Name = user.Name };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            await LoadAsync(user);
            return Page();
        }

        // Save the NAME (its own button)
        public async Task<IActionResult> OnPostSaveProfileAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            if (Input.Name != user.Name)
            {
                user.Name = Input.Name;
                await _userManager.UpdateAsync(user);
                await _signInManager.RefreshSignInAsync(user);
            }

            TempData["Flash"] = "Your name has been updated.";
            TempData["FlashType"] = "success";
            return RedirectToPage();
        }

        // Upload/replace the PICTURE (posts immediately on file pick)
        public async Task<IActionResult> OnPostUploadPictureAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            if (PictureUpload == null || PictureUpload.Length == 0)
            {
                // no image selected
                TempData["Flash"] = "No image was selected.";
                TempData["FlashType"] = "error";
                return RedirectToPage();
            }

            var result = await _profilePictureService.SaveAsync(PictureUpload);
            if (!result.Success)
            {
                // Wrong type / too big
                // bad file
                TempData["Flash"] = result.ErrorMessage;
                TempData["FlashType"] = "error";
                return RedirectToPage();
            }

            var oldPath = user.ProfilePicture;
            user.ProfilePicture = result.Path;
            await _userManager.UpdateAsync(user);
            _profilePictureService.Delete(oldPath);   // remove previous file, if any
            await _signInManager.RefreshSignInAsync(user);

            TempData["Flash"] = "Your profile picture has been updated.";
            TempData["FlashType"] = "success";
            return RedirectToPage();
        }

        // Remove the PICTURE (immediate)
        public async Task<IActionResult> OnPostRemovePictureAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

            if (!string.IsNullOrWhiteSpace(user.ProfilePicture))
            {
                _profilePictureService.Delete(user.ProfilePicture);
                user.ProfilePicture = null;
                await _userManager.UpdateAsync(user);
                await _signInManager.RefreshSignInAsync(user);
                TempData["Flash"] = "Your profile picture has been removed.";
                TempData["FlashType"] = "success";
            }

            return RedirectToPage();
        }
    }
}