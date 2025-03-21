using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Showcase.Areas.Identity.Data;

namespace Showcase.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            return Ok(new { username = user.UserName });
        }
    }
}
