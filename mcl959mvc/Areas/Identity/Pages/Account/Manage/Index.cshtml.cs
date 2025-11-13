// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using mcl959mvc.Data;
using mcl959mvc.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

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
            [Display(Name = "Receive Event Emails (optional)")]
            public bool GetEmailUpdates { get; set; } = false;

            [EmailAddress]
            [Display(Name = "Work Email")]
            public string WorkEmail { get; set; }

            [Display(Name = "Work Address")]
            public string WorkAddress { get; set; }

            [Display(Name = "Work Phone")]
            public string WorkPhone { get; set; }
        }

        // Helpers: normalize to digits and format for display (US 10-digit, optional leading 1)
        private static string DigitsOnly(string s) =>
            string.IsNullOrWhiteSpace(s) ? "" : new string(s.Where(char.IsDigit).ToArray());

        private static string NormalizePhone(string s)
        {
            var d = DigitsOnly(s ?? "");
            if (d.Length == 11 && d.StartsWith("1")) d = d[1..];
            return d.Length == 10 ? d : "";
        }

        private static string FormatPhone(string s)
        {
            var d = NormalizePhone(s ?? "");
            if (string.IsNullOrEmpty(d)) return s ?? "";
            return $"({d[..3]}) {d.Substring(3, 3)}-{d[6..]}";
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
                PhoneNumber = FormatPhone(phoneNumber),
                GetEmailUpdates = user.GetEmailUpdates,
                PersonalEmail = userName,
                PersonalAddress = roster?.PersonalAddress ?? "",
                PersonalPhone = FormatPhone(roster?.PersonalPhone ?? phoneNumber),
                WorkEmail = roster?.WorkEmail ?? "",
                WorkAddress = roster?.WorkAddress ?? "",
                WorkPhone = FormatPhone(roster?.WorkPhone ?? "")
            };
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

            // Normalize phones to digits-only
            var userPhoneDigits = NormalizePhone(Input.PhoneNumber);
            var personalDigits = NormalizePhone(Input.PersonalPhone);
            var workDigits = NormalizePhone(Input.WorkPhone);

            // Basic validation: require 10-digit for user phone; roster phones optional but must be valid if provided
            if (string.IsNullOrEmpty(userPhoneDigits))
            {
                ModelState.AddModelError("Input.PhoneNumber", "Please enter a valid 10-digit phone number.");
            }
            if (!string.IsNullOrWhiteSpace(Input.PersonalPhone) && string.IsNullOrEmpty(personalDigits))
            {
                ModelState.AddModelError("Input.PersonalPhone", "Please enter a valid 10-digit phone number or leave blank.");
            }
            if (!string.IsNullOrWhiteSpace(Input.WorkPhone) && string.IsNullOrEmpty(workDigits))
            {
                ModelState.AddModelError("Input.WorkPhone", "Please enter a valid 10-digit phone number or leave blank.");
            }

            if (!ModelState.IsValid)
            {
                // Reformat for display before returning
                Input.PhoneNumber = FormatPhone(Input.PhoneNumber);
                Input.PersonalPhone = FormatPhone(Input.PersonalPhone);
                Input.WorkPhone = FormatPhone(Input.WorkPhone);
                await LoadAsync(user);
                return Page();
            }

            // Update Identity phone if changed
            var currentUserPhone = await _userManager.GetPhoneNumberAsync(user);
            if (userPhoneDigits != (currentUserPhone ?? ""))
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, userPhoneDigits);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            // Save GetEmailUpdates
            if (user.GetEmailUpdates != Input.GetEmailUpdates)
            {
                user.GetEmailUpdates = Input.GetEmailUpdates;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    StatusMessage = "Error saving email updates preference.";
                    return RedirectToPage();
                }
            }

            // Update Roster record with normalized digits-only phones
            var roster = await _context.Roster
                .FirstOrDefaultAsync(r => r.PersonalEmail == user.UserName || r.WorkEmail == user.UserName);

            if (roster != null)
            {
                roster.PersonalEmail = user.UserName;
                roster.PersonalAddress = Input.PersonalAddress ?? "";
                roster.PersonalPhone = string.IsNullOrEmpty(personalDigits) ? "" : personalDigits;
                roster.WorkEmail = Input.WorkEmail ?? "";
                roster.WorkAddress = Input.WorkAddress ?? "";
                roster.WorkPhone = string.IsNullOrEmpty(workDigits) ? "" : workDigits;
                await _context.SaveChangesAsync();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}
