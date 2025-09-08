using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;

namespace TinkerGenie.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PreferencesController : ControllerBase
    {
        [HttpGet("{userId}")]
        public IActionResult GetPreferences(string userId)
        {
            try
            {
                return Ok(new 
                {
                    userId = userId,
                    theme = "light",
                    notifications = true,
                    language = "en",
                    promptStyle = "concise"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }
        
        [HttpPost("{userId}")]
        public IActionResult UpdatePreferences(string userId, [FromBody] dynamic preferences)
        {
            return Ok(new { success = true, message = "Preferences updated" });
        }

        [HttpPost]
        public IActionResult SavePreferences([FromBody] dynamic preferences)
        {
            return Ok(new { success = true, message = "Preferences saved successfully" });
        }
    }
}
