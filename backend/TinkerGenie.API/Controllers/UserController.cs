using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Text.Json;

namespace TinkerGenie.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<UserController> _logger;
        
        public UserController(IConfiguration configuration, ILogger<UserController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
            _logger = logger;
        }
        
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUser(string userId)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                
                await using var cmd = new NpgsqlCommand(@"
                    SELECT name, business_name, email, current_day, preferences, metadata
                    FROM user_data 
                    WHERE user_id = @userId", conn);
                
                cmd.Parameters.AddWithValue("userId", userId);
                
                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return Ok(new
                    {
                        name = reader.IsDBNull(0) ? "Leader" : reader.GetString(0),
                        businessName = reader.IsDBNull(1) ? "Your Gym" : reader.GetString(1),
                        email = reader.IsDBNull(2) ? null : reader.GetString(2),
                        currentDay = reader.IsDBNull(3) ? 1 : reader.GetInt32(3),
                        preferences = reader.IsDBNull(4) ? null : reader.GetString(4),
                        metadata = reader.IsDBNull(5) ? null : reader.GetString(5)
                    });
                }
                
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", userId);
                return NotFound();
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] UserData userData)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                
                await using var cmd = new NpgsqlCommand(@"
                    INSERT INTO user_data (user_id, name, business_name, email, current_day)
                    VALUES (@userId, @name, @businessName, @email, @currentDay)
                    ON CONFLICT (user_id) 
                    DO UPDATE SET 
                        name = EXCLUDED.name,
                        business_name = EXCLUDED.business_name,
                        email = EXCLUDED.email,
                        last_active = CURRENT_TIMESTAMP
                    RETURNING id", conn);
                
                cmd.Parameters.AddWithValue("userId", userData.UserId ?? "");
                cmd.Parameters.AddWithValue("name", (object?)userData.Name ?? DBNull.Value);
                cmd.Parameters.AddWithValue("businessName", (object?)userData.BusinessName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("email", (object?)userData.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("currentDay", userData.CurrentDay);
                
                var id = await cmd.ExecuteScalarAsync();
                
                return Ok(new { id, userId = userData.UserId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return BadRequest(new { error = "Failed to create user" });
            }
        }
        
        [HttpPost("{userId}/progress")]
        public async Task<IActionResult> UpdateProgress(string userId, [FromBody] ProgressUpdate update)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                
                await using var cmd = new NpgsqlCommand(@"
                    UPDATE user_data 
                    SET current_day = @currentDay, 
                        last_active = CURRENT_TIMESTAMP
                    WHERE user_id = @userId", conn);
                
                cmd.Parameters.AddWithValue("userId", userId);
                cmd.Parameters.AddWithValue("currentDay", update.CurrentDay);
                
                var rowsAffected = await cmd.ExecuteNonQueryAsync();
                
                if (rowsAffected > 0)
                    return Ok(new { success = true });
                    
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating progress for user {UserId}", userId);
                return BadRequest();
            }
        }
    }
    
    public class UserData
    {
        public string? UserId { get; set; }
        public string? Name { get; set; }
        public string? BusinessName { get; set; }
        public string? Email { get; set; }
        public int CurrentDay { get; set; } = 1;
    }
    
    public class ProgressUpdate
    {
        public int CurrentDay { get; set; }
    }
}
