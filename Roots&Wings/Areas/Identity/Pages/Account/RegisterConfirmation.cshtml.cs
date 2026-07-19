// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Roots_Wings.Models;
using Roots_Wings.Services.Interfaces;
using System;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Roots_Wings.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _sender;

        public RegisterConfirmationModel(UserManager<ApplicationUser> userManager, IEmailService sender)
        {
            _userManager = userManager;
            _sender = sender;
        }

        public string Email { get; set; }
        public string EmailConfirmationUrl { get; set; }

        public async Task<IActionResult> OnGetAsync(string email, string returnUrl = null)
        {
            if (email == null)
            {
                return RedirectToPage("/Index");
            }
            returnUrl = returnUrl ?? Url.Content("~/");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"Unable to load user with email '{email}'.");
            }

            Email = email;

            // Only send a confirmation email if the user is not already confirmed
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                    protocol: Request.Scheme);

                EmailConfirmationUrl = HtmlEncoder.Default.Encode(callbackUrl);

                await _sender.SendConfirmationEmailAsync(Email, "Confirm your email", EmailConfirmationUrl);
            }

            return Page();
        }
    }
}
