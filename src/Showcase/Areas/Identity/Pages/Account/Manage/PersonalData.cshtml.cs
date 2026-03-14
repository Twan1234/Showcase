// Licensed to the .NET Foundation under one or more agreements.
#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Showcase.Areas.Identity.Data;

namespace Showcase.Areas.Identity.Pages.Account.Manage
{
    /// <summary>
    /// V8.3.2 ASVS: Users can download or delete their personal data (GDPR).
    /// </summary>
    public class PersonalDataModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PersonalDataModel> _logger;

        public PersonalDataModel(
            UserManager<ApplicationUser> userManager,
            ILogger<PersonalDataModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostDownloadPersonalDataAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            _logger.LogInformation("User {UserId} requested their personal data.", _userManager.GetUserId(User));

            var personalData = new Dictionary<string, string>
            {
                ["Id"] = user.Id,
                ["UserName"] = user.UserName ?? "",
                ["Email"] = user.Email ?? "",
                ["EmailConfirmed"] = user.EmailConfirmed.ToString(),
                ["PhoneNumber"] = user.PhoneNumber ?? "",
                ["TwoFactorEnabled"] = user.TwoFactorEnabled.ToString()
            };
            if (user is ApplicationUser appUser)
                personalData["Name"] = appUser.Name ?? "";

            var json = JsonSerializer.Serialize(personalData, new JsonSerializerOptions { WriteIndented = true });
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);

            return File(bytes, "application/json", "PersonalData.json");
        }
    }
}
