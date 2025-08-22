// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using mcl959mvc.Data;
using mcl959mvc.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace mcl959mvc.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly Mcl959DbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        public bool HasRosterMatch { get; set; }

        public IndexModel(
            Mcl959DbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }

            // Roster fields
            [EmailAddress]
            [Display(Name = "Personal Email")]
            public string PersonalEmail { get; set; }

            [Display(Name = "Personal Address")]
            public string PersonalAddress { get; set; }

            [Display(Name = "Personal Phone")]
            public string PersonalPhone { get; set; }

            [EmailAddress]
            [Display(Name = "Work Email")]
            public string WorkEmail { get; set; }

            [Display(Name = "Work Address")]
            public string WorkAddress { get; set; }

            [Display(Name = "Work Phone")]
            public string WorkPhone { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            // Find matching Roster record
            var roster = await _context.Roster
                .FirstOrDefaultAsync(r => r.PersonalEmail == userName || r.WorkEmail == userName);

            HasRosterMatch = roster != null;

            Username = userName;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber
            };
            if (HasRosterMatch)
            {
                Input.PersonalEmail = roster.PersonalEmail;
                Input.PersonalAddress = roster.PersonalAddress;
                Input.PersonalPhone = roster.PersonalPhone;
                Input.WorkEmail = roster.WorkEmail;
                Input.WorkAddress = roster.WorkAddress;
                Input.WorkPhone = roster.WorkPhone;
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            // Update Roster record
            var roster = await _context.Roster
                .FirstOrDefaultAsync(r => r.PersonalEmail == user.UserName || r.WorkEmail == user.UserName);

            if (roster != null)
            {
                roster.PersonalEmail = Input.PersonalEmail;
                roster.PersonalAddress = Input.PersonalAddress;
                roster.PersonalPhone = Input.PersonalPhone;
                roster.WorkEmail = Input.WorkEmail;
                roster.WorkAddress = Input.WorkAddress;
                roster.WorkPhone = Input.WorkPhone;
                await _context.SaveChangesAsync();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}
